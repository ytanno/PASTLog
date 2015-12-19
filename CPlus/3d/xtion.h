#include <OpenNI.h>

// OpenNI2 で Xtion からデータを取ってくるクラス
class Xtion
{
public:
	Xtion(openni::SensorType type, int width, int height, int fps);
	~Xtion();

	template <typename T>
	const T* getData()
	{
		openni::VideoFrameRef frame;
		videoStream_.readFrame(&frame);

		if (!frame.isValid()) {
			return nullptr;
		}

		return static_cast<const T*>(frame.getData());
	}

	int getResolutionX() const;
	const openni::VideoStream& getStream() const;

private:
	static void Initialize();
	static void Shutdown();

	void createStream();
	void start();

	static openni::Device Device;
	static int Count;

	const openni::SensorType sensorType_;
	const int width_, height_, fps_;
	openni::VideoStream videoStream_;
};