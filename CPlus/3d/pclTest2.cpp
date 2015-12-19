// pclTest2.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include "xtion.h"

#include <limits>
#include <fstream>
#include <vector>
#include <Eigen/Core>
#include <iostream>
#include <opencv2\opencv.hpp>
#include <boost/make_shared.hpp>



#include <pcl/filters/statistical_outlier_removal.h>
#include <pcl/visualization/cloud_viewer.h>


#include <pcl/PCLPointCloud2.h>
#include <pcl/filters/approximate_voxel_grid.h>

#include <pcl/point_types.h>
#include <pcl/point_cloud.h>
#include <pcl/io/pcd_io.h>
#include <pcl/kdtree/kdtree_flann.h>
#include <pcl/filters/passthrough.h>
#include <pcl/filters/voxel_grid.h>
#include <pcl/features/normal_3d.h>
#include <pcl/features/fpfh.h>
#include <pcl/registration/ia_ransac.h>
#include <pcl/registration/icp.h>

const int SaveImgCount = 2;
const std::string ImgFolder = "2dData/";
const std::string DataFolder = "3dData/";
const std::string PrepareFolder = "pData/";
const std::string SacIAFolder = "sacia/";
const std::string FileName = "p";
//last
const std::string DstFolder = "icp/";




class FeatureCloud
{

public:
	// A bit of shorthand
	typedef pcl::PointCloud<pcl::PointXYZ> PointCloud;
	typedef pcl::PointCloud<pcl::Normal> SurfaceNormals;
	typedef pcl::PointCloud<pcl::FPFHSignature33> LocalFeatures;
	typedef pcl::search::KdTree<pcl::PointXYZ> SearchMethod;

	FeatureCloud() :
		search_method_xyz_(new SearchMethod),
		//normal_radius_(0.02f),
		//feature_radius_(0.02f)
		normal_radius_(0.03f),
		feature_radius_(0.08)
	{}

	~FeatureCloud() {}

	// Process the given cloud
	void
		setInputCloud(PointCloud::Ptr xyz)
	{
		xyz_ = xyz;
		processInput();
	}

	// Load and process the cloud in the given PCD file
	void
		loadInputCloud(const std::string &pcd_file)
	{
		xyz_ = PointCloud::Ptr(new PointCloud);
		pcl::io::loadPCDFile(pcd_file, *xyz_);
		processInput();
	}

	// Get a pointer to the cloud 3D points
	PointCloud::Ptr
		getPointCloud() const
	{
		return (xyz_);
	}

	// Get a pointer to the cloud of 3D surface normals
	SurfaceNormals::Ptr
		getSurfaceNormals() const
	{
		return (normals_);
	}

	// Get a pointer to the cloud of feature descriptors
	LocalFeatures::Ptr
		getLocalFeatures() const
	{
		return (features_);
	}

protected:
	// Compute the surface normals and local features
	void
		processInput()
	{
		computeSurfaceNormals();
		computeLocalFeatures();
	}

	// Compute the surface normals
	void
		computeSurfaceNormals()
	{
		normals_ = SurfaceNormals::Ptr(new SurfaceNormals);

		pcl::NormalEstimation<pcl::PointXYZ, pcl::Normal> norm_est;
		norm_est.setInputCloud(xyz_);
		norm_est.setSearchMethod(search_method_xyz_);
		norm_est.setRadiusSearch(normal_radius_);
		norm_est.compute(*normals_);
	}

	// Compute the local feature descriptors
	void
		computeLocalFeatures()
	{
		features_ = LocalFeatures::Ptr(new LocalFeatures);

		pcl::FPFHEstimation<pcl::PointXYZ, pcl::Normal, pcl::FPFHSignature33> fpfh_est;
		fpfh_est.setInputCloud(xyz_);
		fpfh_est.setInputNormals(normals_);
		fpfh_est.setSearchMethod(search_method_xyz_);
		fpfh_est.setRadiusSearch(feature_radius_);
		fpfh_est.compute(*features_);
	}

private:
	// Point cloud data
	PointCloud::Ptr xyz_;
	SurfaceNormals::Ptr normals_;
	LocalFeatures::Ptr features_;
	SearchMethod::Ptr search_method_xyz_;

	// Parameters
	float normal_radius_;
	float feature_radius_;
};

class TemplateAlignment
{
public:

	// A struct for storing alignment results
	struct Result
	{
		float fitness_score;
		Eigen::Matrix4f final_transformation;
		EIGEN_MAKE_ALIGNED_OPERATOR_NEW
	};

	TemplateAlignment() :
		//min_sample_distance_(0.05f),
		//max_correspondence_distance_(0.01f*0.01f),
		//nr_iterations_(500)

		min_sample_distance_(0.05f),
		max_correspondence_distance_(0.01f*0.01f),
		nr_iterations_(50)

	{
		// Intialize the parameters in the Sample Consensus Intial Alignment (SAC-IA) algorithm
		sac_ia_.setMinSampleDistance(min_sample_distance_);
		sac_ia_.setMaxCorrespondenceDistance(max_correspondence_distance_);
		sac_ia_.setMaximumIterations(nr_iterations_);
	}

	~TemplateAlignment() {}

	// Set the given cloud as the target to which the templates will be aligned
	void
		setTargetCloud(FeatureCloud &target_cloud)
	{
		target_ = target_cloud;
		sac_ia_.setInputTarget(target_cloud.getPointCloud());
		sac_ia_.setTargetFeatures(target_cloud.getLocalFeatures());
	}

	// Add the given cloud to the list of template clouds
	void
		addTemplateCloud(FeatureCloud &template_cloud)
	{
		templates_.push_back(template_cloud);
	}

	// Align the given template cloud to the target specified by setTargetCloud ()
	void
		align(FeatureCloud &template_cloud, TemplateAlignment::Result &result)
	{
		sac_ia_.setInputCloud(template_cloud.getPointCloud());
		sac_ia_.setSourceFeatures(template_cloud.getLocalFeatures());

		pcl::PointCloud<pcl::PointXYZ> registration_output;
		sac_ia_.align(registration_output);

		result.fitness_score = (float)sac_ia_.getFitnessScore(max_correspondence_distance_);
		result.final_transformation = sac_ia_.getFinalTransformation();
	}

	// Align all of template clouds set by addTemplateCloud to the target specified by setTargetCloud ()
	void
		alignAll(std::vector<TemplateAlignment::Result, Eigen::aligned_allocator<Result> > &results)
	{
		results.resize(templates_.size());
		for (size_t i = 0; i < templates_.size(); ++i)
		{
			align(templates_[i], results[i]);
		}
	}

	// Align all of template clouds to the target cloud to find the one with best alignment score
	int
		findBestAlignment(TemplateAlignment::Result &result)
	{
		// Align all of the templates to the target cloud
		std::vector<Result, Eigen::aligned_allocator<Result> > results;
		alignAll(results);

		// Find the template with the best (lowest) fitness score
		float lowest_score = std::numeric_limits<float>::infinity();
		int best_template = 0;
		for (size_t i = 0; i < results.size(); ++i)
		{
			const Result &r = results[i];
			if (r.fitness_score < lowest_score)
			{
				lowest_score = r.fitness_score;
				best_template = (int)i;
			}
		}

		// Output the best alignment
		result = results[best_template];
		return (best_template);
	}

private:
	// A list of template clouds and the target to which they will be aligned
	std::vector<FeatureCloud> templates_;
	FeatureCloud target_;

	// The Sample Consensus Initial Alignment (SAC-IA) registration routine and its parameters
	pcl::SampleConsensusInitialAlignment<pcl::PointXYZ, pcl::PointXYZ, pcl::FPFHSignature33> sac_ia_;
	float min_sample_distance_;
	float max_correspondence_distance_;
	int nr_iterations_;
};

// Align a collection of object templates to a sample point cloud


cv::Rect DivArea(cv::Mat img, int filterArea)
{
	//cv::Mat img = cv::imread(ImgFolder + FileName + "0.jpg", 1); //3チャンネルカラー画像で読み込む;

	//グレースケール
	cv::Mat grayImage, binImage;
	cv::cvtColor(img, grayImage, CV_BGR2GRAY);

	//2値化
	cv::threshold(grayImage, binImage, 0.0, 255.0, CV_THRESH_BINARY_INV | CV_THRESH_OTSU);

	//輪郭の座標リスト
	std::vector< std::vector< cv::Point > > contours;

	//輪郭取得
	cv::findContours(binImage, contours, CV_RETR_LIST, CV_CHAIN_APPROX_NONE);

	// 検出された輪郭線を緑で描画
	/*
	for (auto contour = contours.begin(); contour != contours.end(); contour++){
	cv::polylines(imgIn, *contour, true, cv::Scalar(0, 255, 0), 2);
	}
	*/



	//輪郭の数
	int roiCnt = 0;

	//輪郭のカウント   
	int i = 0;
	int maxX = 0, maxY = 0, minX = img.rows, minY = img.cols;
	
	for (auto contour = contours.begin(); contour != contours.end(); contour++)
	{
		std::vector< cv::Point > approx;

		//輪郭を直線近似する
		cv::approxPolyDP(cv::Mat(*contour),
			approx, 0.001 * cv::arcLength(*contour, true), true);

		// 近似の面積が一定以上なら取得
		double area = cv::contourArea(approx);

		if (area > filterArea)
		{
			for (std::vector<cv::Point>::iterator i = approx.begin(); i != approx.end(); i++)
			{
				if (i->x > maxX) maxX = i->x;
				if (i->x < minX) minX = i->x;
				if (i->y > maxY) maxY = i->y;
				if (i->y < minY) minY = i->y;
			}
		}

		i++;
	}

	
	if (maxX - minX < 0 || maxY - minY < 0) 
	{
		cv::Rect  mask(0, 0, img.rows, img.cols);
		return mask;
	}
	else 
	{
		cv::Rect  mask(minX, minY, maxX - minX, maxY - minY);
		return mask;
	}
	
	//cv::Mat roi = imgIn(mask);
	
}



void SaveXtion()
{
	int nowImg = 0;
	bool save = false;

	const int WIDTH = 320, HEIGHT = 240, FPS = 30;
	// CloudViewer を生成
	// （表示は別スレッドで実行される）
	pcl::visualization::CloudViewer viewer("OpenNI Viewer");

	// Xtion からデータを取ってくるクラスのインスタンス（後述）
	Xtion sensorColor(openni::SensorType::SENSOR_COLOR, WIDTH, HEIGHT, FPS);
	Xtion sensorDepth(openni::SensorType::SENSOR_DEPTH, WIDTH, HEIGHT, FPS);

	IplImage* img = cvCreateImage(cvSize(WIDTH, HEIGHT), IPL_DEPTH_8U, 3);
	cv::Rect mask(0, 0, WIDTH, HEIGHT);


	// 最新のポイントクラウドを表示し続ける
	for (int i = 0;; ++i) 
	{
		// CloudViewer に与える PointCloud 
		boost::shared_ptr<pcl::PointCloud<pcl::PointXYZRGB>>
			cloud(new pcl::PointCloud<pcl::PointXYZRGB>());

		// ポイントクラウドの大きさをセット
		cloud->width = WIDTH;
		cloud->height = HEIGHT;
		cloud->is_dense = false;
		cloud->points.resize(cloud->height * cloud->width);

		// 最新の RGB / デプス情報を取ってくる
		const auto color = sensorColor.getData<openni::RGB888Pixel>();
		const auto depth = sensorDepth.getData<openni::DepthPixel>();
		const int resX = sensorColor.getResolutionX();

		// データを1つずつみてポイントクラウドに詰めていく
		for (int j = 0; j < HEIGHT; ++j)
		{
			for (int i = 0; i < WIDTH; ++i)
			{

				// convertDepthToWorld を利用すると、実際の世界の座標に変換してくれる
				const auto z = depth[j * resX + i];
				const int offsetX = 10;
				const int index = j * resX + (i + offsetX < WIDTH ? i + offsetX : WIDTH);
				const auto rgb = color[index];

				if (!save)
				{
					//2m以内のもの以外 または　mask範囲外 除外
					//密度の減少
					if (i % 2 == 0 && j % 2 == 0 &&
						z < 1000 &&
						mask.x < i && mask.x + mask.width > i &&
						mask.y < j && mask.y + mask.height > j)
					{

						float wx, wy, wz;
						openni::CoordinateConverter::convertDepthToWorld(sensorDepth.getStream(), i, j, z, &wx, &wy, &wz);

						// 実際の世界の座標を Point につめる
						pcl::PointXYZRGB point;
						const float millimeterToMeter = 0.001f;
						point.x = wx * millimeterToMeter;
						point.y = wy * millimeterToMeter;
						point.z = wz * millimeterToMeter;


						// RGB カメラとデプスカメラの位置がずれているので適当にオフセットして色を Point につめる
						point.r = rgb.r;
						point.g = rgb.g;
						point.b = rgb.b;
						cloud->push_back(point);

						//img
						img->imageData[img->widthStep * j + i * 3] = rgb.b;
						img->imageData[img->widthStep * j + i * 3 + 1] = rgb.g;
						img->imageData[img->widthStep * j + i * 3 + 2] = rgb.r;
					}

					else
					{
						img->imageData[img->widthStep * j + i * 3] = 255;
						img->imageData[img->widthStep * j + i * 3 + 1] = 255;
						img->imageData[img->widthStep * j + i * 3 + 2] = 255;
					}
				}
			}
		}

		//Enter で　mask 生成
		if (GetKeyState(VK_RETURN) < 0)
		{
			cv::Mat m(img);
			mask = DivArea(m, 2000);
		}

		if (GetKeyState(VK_SHIFT) < 0)
		{
			mask = cv::Rect (0, 0, WIDTH, HEIGHT);
		}
		


		// CloudViewer で点群を表示
		viewer.showCloud(cloud);
		
		

		// ESC キーで終了
		if (GetKeyState(VK_ESCAPE) < 0) 
		{
			
			save = true;
			char buf[5];
			itoa(nowImg, buf, 10);
			std::string numStr = std::string(buf);

			//save 3d
			std::string save3dPath = DataFolder + FileName + numStr + ".pcd";
			pcl::io::savePCDFileASCII(save3dPath , *cloud);

			//save 2d
			std::string save2dPath = ImgFolder + FileName + numStr + ".jpg";
			const char *d2File = save2dPath.c_str();
			cvSaveImage(d2File, img);

			

			save = false;
			if (nowImg == SaveImgCount -1) 
			{
				cvReleaseImage(&img);
				break;
			}
			nowImg++;
			
		}
	}
}


pcl::PointCloud<pcl::PointXYZ>::Ptr OutlierFilter(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud)
{

	pcl::PointCloud<pcl::PointXYZ>::Ptr cloud_filtered(new pcl::PointCloud<pcl::PointXYZ>);
	//cout << "Cloud before filtering: " << endl;
	//cout << *cloud << endl;

	// Create the filtering object
	pcl::StatisticalOutlierRemoval<pcl::PointXYZ> sor;
	sor.setInputCloud(cloud);
	sor.setMeanK(50);
	sor.setStddevMulThresh(1.0);
	sor.filter(*cloud_filtered);

	//cout << "Cloud after filtering: " << endl;
	//cout << *cloud_filtered << endl;

	//sor.setNegative(true);
	//sor.filter(*cloud_filtered);
	//writer.write<pcl::PointXYZRGB>("test_outliers.pcd", *cloud_filtered, false);

	return cloud_filtered;
}

pcl::PointCloud<pcl::PointXYZ>::Ptr DownSampling(pcl::PointCloud<pcl::PointXYZ>::Ptr cloud)
{
	//std::cout << "PointCloud before filtering: " << cloud->width * cloud->height
	//	<< " data points (" << pcl::getFieldsList(*cloud) << ").";

	//const float voxel_grid_size = 0.005f;
	//const float voxel_grid_size = 0.01f;
	const float voxel_grid_size = 0.025f;
	pcl::VoxelGrid<pcl::PointXYZ> vox_grid;
	vox_grid.setInputCloud(cloud);
	vox_grid.setLeafSize(voxel_grid_size, voxel_grid_size, voxel_grid_size);
	pcl::PointCloud<pcl::PointXYZ>::Ptr cloud_filtered(new pcl::PointCloud<pcl::PointXYZ>);
	vox_grid.filter(*cloud_filtered);

	//std::cout << "PointCloud after filtering: " << cloud_filtered->width * cloud_filtered->height
	//	<< " data points (" << pcl::getFieldsList(*cloud_filtered) << ").";

	return cloud_filtered;
}

pcl::PointCloud<pcl::PointXYZRGB>::Ptr DownSamplingColor(pcl::PointCloud<pcl::PointXYZRGB>::Ptr cloud)
{
	//std::cout << "PointCloud before filtering: " << cloud->width * cloud->height
	//	<< " data points (" << pcl::getFieldsList(*cloud) << ").";

	//const float voxel_grid_size = 0.005f;
	//const float voxel_grid_size = 0.01f;
	const float voxel_grid_size = 0.025f;
	pcl::VoxelGrid<pcl::PointXYZRGB> vox_grid;
	vox_grid.setInputCloud(cloud);
	vox_grid.setLeafSize(voxel_grid_size, voxel_grid_size, voxel_grid_size);
	pcl::PointCloud<pcl::PointXYZRGB>::Ptr cloud_filtered(new pcl::PointCloud<pcl::PointXYZRGB>);
	vox_grid.filter(*cloud_filtered);

	//std::cout << "PointCloud after filtering: " << cloud_filtered->width * cloud_filtered->height
	//	<< " data points (" << pcl::getFieldsList(*cloud_filtered) << ").";

	return cloud_filtered;
}





void Prepare()
{
	cout << "Prepare Start" << endl;

	for (int i = 0; i < SaveImgCount; i++)
	{
		char buf[5];
		itoa(i, buf, 10);
		std::string numStr = std::string(buf);
		std::string fName = FileName + numStr + ".pcd";
		std::string readPath = DataFolder + fName;

		//read
		pcl::PointCloud<pcl::PointXYZ>::Ptr cloud(new pcl::PointCloud<pcl::PointXYZ>);
		pcl::PCDReader reader;
		reader.read<pcl::PointXYZ>(readPath, *cloud);

		cout << fName + " point ";
		cout << cloud->points.size();

		
		int loopCount = 0;
		/*
		//loop
		//for (int j = 0; j < 3; j++)
		while (true)
		{
			loopCount++;
			if (loopCount > 2) break;

			//downSampling
			cloud = DownSampling(cloud);
		}
		*/

		cloud = DownSampling(cloud);
		//filtered
		cloud = OutlierFilter(cloud);

		cout <<  " -> " << cloud->points.size() <<  "  loopCount:" << loopCount <<  endl;

		//save
		std::string savePath = PrepareFolder + FileName + numStr + ".pcd";
		pcl::io::savePCDFileASCII(savePath, *cloud);

		//cout << fName + " End" << endl;
	}

	cout << "Prepare End" << endl;

}



void SACIA()
{
	cout << "Values less than 0.00002 are good" << endl;
	cout << "SACIA Start" << endl;

	FeatureCloud target_cloud;
	std::vector<FeatureCloud> object_templates;
	object_templates.resize(0);

	for (int i = 0; i < SaveImgCount; i++)
	{
		//read
		char buf[5];
		itoa(i, buf, 10);
		std::string numStr = std::string(buf);
		std::string readPath = PrepareFolder + FileName + numStr + ".pcd";

		pcl::PointCloud<pcl::PointXYZ>::Ptr cloud(new pcl::PointCloud<pcl::PointXYZ>);
		pcl::PCDReader reader;
		reader.read<pcl::PointXYZ>(readPath, *cloud);


		if (i == 0)
		{
			target_cloud.setInputCloud(cloud);
		}
		else
		{
			//add objectTemplate
			FeatureCloud template_cloud;
			template_cloud.setInputCloud(cloud);
			object_templates.push_back(template_cloud);
		}
	}


	// Set the TemplateAlignment inputs
	TemplateAlignment template_align;
	for (size_t i = 0; i < object_templates.size(); ++i)
	{
		template_align.addTemplateCloud(object_templates[i]);
	}
	template_align.setTargetCloud(target_cloud);


	// Find the best template alignment
	TemplateAlignment::Result best_alignment;
	int best_index = template_align.findBestAlignment(best_alignment);

	std::vector<TemplateAlignment::Result, 
		Eigen::aligned_allocator<TemplateAlignment::Result> > results;
	template_align.alignAll(results);

	cout << "Base 3D Data " <<  PrepareFolder + FileName + "0.pcd" << endl;
	//cout << "Best Much index is " << best_index <<  endl;

	for (size_t i = 0; i < results.size(); ++i)
	{
		char buf[5];
		itoa(i + 1, buf, 10);
		std::string numStr = std::string(buf);

		std::string nameIndex = PrepareFolder + FileName + numStr + ".pcd";
		cout << "Compare " << nameIndex << endl;
		const FeatureCloud &comp_data = object_templates[i];
		const TemplateAlignment::Result &r = results[i];
		
		
		cout << "fitness score: " << r.fitness_score << endl;

		// Print the rotation matrix and translation vector
		Eigen::Matrix3f rotation = r.final_transformation.block<3, 3>(0, 0);
		Eigen::Vector3f translation = r.final_transformation.block<3, 1>(0, 3);

		printf("\n");
		printf("    | %6.3f %6.3f %6.3f | \n", rotation(0, 0), rotation(0, 1), rotation(0, 2));
		printf("R = | %6.3f %6.3f %6.3f | \n", rotation(1, 0), rotation(1, 1), rotation(1, 2));
		printf("    | %6.3f %6.3f %6.3f | \n", rotation(2, 0), rotation(2, 1), rotation(2, 2));
		printf("\n");
		printf("t = < %0.3f, %0.3f, %0.3f >\n", translation(0), translation(1), translation(2));

		// Save the aligned template for visualization
		pcl::PointCloud<pcl::PointXYZ> transformed_cloud;
		pcl::transformPointCloud(*comp_data.getPointCloud(), transformed_cloud, r.final_transformation);
		pcl::io::savePCDFileASCII(SacIAFolder + FileName + numStr + ".pcd", transformed_cloud);
	}

	cout << "SACIA End" << endl;
}


void ICP()
{
	cout << endl;
	cout << endl;
	cout << "ICP Start" << endl;

	pcl::PointCloud<pcl::PointXYZ>::Ptr baseCloud(new pcl::PointCloud<pcl::PointXYZ>);
	pcl::PCDReader reader;
	reader.read<pcl::PointXYZ>(PrepareFolder + FileName + "0.pcd", *baseCloud);

	for (int i = 1; i < SaveImgCount; i++)
	{
		char buf[5];
		itoa(i, buf, 10);
		std::string numStr = std::string(buf);
		std::string readPath = SacIAFolder + FileName + numStr + ".pcd";

		pcl::PointCloud<pcl::PointXYZ>::Ptr inputCloud(new pcl::PointCloud<pcl::PointXYZ>);
		reader.read<pcl::PointXYZ>(readPath, *inputCloud);

		pcl::IterativeClosestPoint<pcl::PointXYZ, pcl::PointXYZ> icp;
		icp.setInputCloud(baseCloud);
		icp.setInputTarget(inputCloud);
		pcl::PointCloud<pcl::PointXYZ> finalPoint;
		icp.align(finalPoint);

		//std::cout << "has converged:" << icp.hasConverged() << " score: " <<
	
		cout << readPath << "  FitnessScore:" << icp.getFitnessScore() << std::endl;
		std::cout << icp.getFinalTransformation() << std::endl;

		std::string savePath = DstFolder + FileName + numStr + ".pcd";
		pcl::io::savePCDFileASCII(savePath, finalPoint);
	}




	cout << "ICP End" << endl;
}




void ViewResult()
{
	pcl::PointCloud<pcl::PointXYZRGB> cloud_all;
	for (int i = 0; i < SaveImgCount; i++)
	{
		std::string dirPath = "";
		if (i == 0) dirPath = PrepareFolder;
		//else dirPath = DstFolder;
		else dirPath = SacIAFolder;

		char buf[5];
		itoa(i, buf, 10);
		std::string numStr = std::string(buf);
		std::string readFilePath = dirPath + FileName + numStr + ".pcd";
		pcl::PointCloud<pcl::PointXYZ>::Ptr cloud(new pcl::PointCloud<pcl::PointXYZ>);
		pcl::PCDReader reader;
		reader.read(readFilePath, *cloud);
		pcl::PointCloud<pcl::PointXYZRGB>::Ptr cloudRGB(new pcl::PointCloud<pcl::PointXYZRGB>);

		int max = 255;
		int min = 0;
		copyPointCloud(*cloud, *cloudRGB);
		int randColor = min + rand() % (max - min); // MIN以上MAX未満の乱数を生成

		for (int j = 0; j < cloudRGB->points.size(); j++)
		{
			cloudRGB->points[j].r = 0;
			cloudRGB->points[j].b = 0;
			cloudRGB->points[j].g = 0;

			if (i == 0) cloudRGB->points[j].r = 255;
			else if (i == 1) cloudRGB->points[j].g = 255;
			else if (i == 2) cloudRGB->points[j].b = 255;
			else 
			{
				cloudRGB->points[j].r = randColor;
				cloudRGB->points[j].b = randColor;
			}
		}

		cloud_all += *cloudRGB;
	}


	pcl::io::savePCDFileASCII("last.pcd", cloud_all);

	//last
	pcl::PointCloud<pcl::PointXYZRGB>::Ptr lastCloud(new pcl::PointCloud<pcl::PointXYZRGB>);
	pcl::PCDReader reader;
	reader.read("last.pcd", *lastCloud);

	lastCloud = DownSamplingColor(lastCloud);


	boost::shared_ptr<pcl::visualization::PCLVisualizer> viewer(new pcl::visualization::PCLVisualizer("3D Viewer"));
	viewer->setBackgroundColor(255, 255, 255);
	pcl::visualization::PointCloudColorHandlerRGBField<pcl::PointXYZRGB> rgb(lastCloud);
	viewer->addPointCloud<pcl::PointXYZRGB>(lastCloud, rgb, "sample cloud");
	viewer->setPointCloudRenderingProperties(pcl::visualization::PCL_VISUALIZER_POINT_SIZE, 3, "sample cloud");
	viewer->addCoordinateSystem(0);
	viewer->initCameraParameters();

	while (!viewer->wasStopped())
	{
		viewer->spinOnce(100);
		boost::this_thread::sleep(boost::posix_time::microseconds(100000));
	}
}



void My3D()
{
	//ref http://tips.hecomi.com/entry/2014/05/11/214401
	SaveXtion();
	Prepare();
	SACIA();
	ICP();
	ViewResult();
}







int _tmain(int argc, _TCHAR* argv[])
{
	My3D();
	return 0;
}

