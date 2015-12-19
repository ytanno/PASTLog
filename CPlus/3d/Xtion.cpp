#include "stdafx.h"
#include "xtion.h"
#include <exception>

openni::Device Xtion::Device;
int Xtion::Count = 0;

Xtion::Xtion(openni::SensorType type, int width, int height, int fps)
	: sensorType_(type), width_(width), height_(height), fps_(fps)
{
	Initialize();

	createStream();
	start();
}

Xtion::~Xtion()
{
	Shutdown();
}

int Xtion::getResolutionX() const
{
	return videoStream_.getVideoMode().getResolutionX();
}

const openni::VideoStream& Xtion::getStream() const
{
	return videoStream_;
}


void Xtion::Initialize()
{
	// ���񂾂� OpenNI �̏�����
	if (Count == 0) {
		openni::OpenNI::initialize();
		if (Device.open(openni::ANY_DEVICE) != openni::STATUS_OK) {
			throw std::exception(openni::OpenNI::getExtendedError());
		}
	}
	++Count;
}

void Xtion::Shutdown()
{
	--Count;

	// �C���X�^���X�����Ȃ��Ȃ����� OpenNI �̏I��
	if (Count == 0) {
		openni::OpenNI::shutdown();
	}
}

void Xtion::createStream()
{
	videoStream_.create(Device, sensorType_);

	auto mode = videoStream_.getVideoMode();
	mode.setResolution(width_, height_);
	mode.setFps(fps_);

	videoStream_.setVideoMode(mode);
}

void Xtion::start()
{
	videoStream_.start();
}