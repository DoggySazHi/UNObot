#include "WoolData.h"
#include "RCONHelper.h"
#include "MatrixHelper.h"
#include <string>
#include <iostream>
#include <filesystem>
#ifndef SIGPIPE
#include <csignal>
#endif
#include <chrono>

#include <opencv2/videoio.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/opencv.hpp>

int main()
{
    signal(SIGPIPE, SIG_IGN);

    WoolData woolData;
    setenv("DISPLAY", "172.19.240.1:0", true);
    std::cout << "Reading from " << std::filesystem::current_path() << '\n';
    cv::Mat video;
    cv::Mat frame;
    cv::VideoCapture cap("bad_apple_pv.mp4");

    if(!cap.isOpened()) {
        std::cout << "Error opening file!" << '\n';
        return 1;
    }

    std::vector<MatrixHelper*> threads;

    auto frameRate = (int) cap.get(cv::CAP_PROP_FPS);
    long framesElapsed, frameCount = 0;
    auto lastTime = std::chrono::steady_clock::now();
    auto startTime = std::chrono::steady_clock::now();
    float fps = 0.0;
    while (true) {
        std::chrono::duration<float> timeDiff = (std::chrono::steady_clock::now() - startTime);
        auto targetFrames = (int) (timeDiff.count() * frameRate);

        if (framesElapsed < targetFrames) {
            // If running too slow
            while (framesElapsed < targetFrames) {
                cap.grab();
                framesElapsed++;
            }
        } else {
            // If running too fast
            std::this_thread::sleep_for(std::chrono::milliseconds((int) ((float) (framesElapsed - targetFrames) / frameRate * 1000.0f)));
        }

        cap >> video;
        if (video.empty())
            break;
        cv::resize(video, frame, cv::Size(), 0.25, 0.25);
        cv::putText(video, "FPS: " + std::to_string(fps), cv::Point(video.cols - 75, video.rows - 5), cv::FONT_HERSHEY_DUPLEX, 0.4, cv::Scalar(0, 0, 255), 1);
        cv::imshow("RCON Video Preview", video);
        if (cv::waitKey(1) == 27) // esc
            break;
        if (threads.empty()) {
            const int THREAD_COUNT = 40;
            auto delta = (int) ceil((float) frame.rows / THREAD_COUNT);
            for(int i = 0; i < THREAD_COUNT; i++) {
                auto temp = new MatrixHelper(frame, i * delta, (i + 1) * delta);
                threads.push_back(temp);
            }
            std::cout << "Spawned " << THREAD_COUNT << " processing threads!\n";
        }

        for(auto &thread : threads)
            thread->signalToThread.notify();
        for(auto &thread : threads)
            thread->signalToMain.wait();
        frameCount++;
        framesElapsed++;

        std::chrono::duration<float> fpsDiffDuration = (std::chrono::steady_clock::now() - lastTime);
        auto fpsDiff = fpsDiffDuration.count();
        if (fpsDiff >= 1.0) {
            fps = (float) frameCount / fpsDiff;
            lastTime = std::chrono::steady_clock::now();
            frameCount = 0;
        }
    }

    cap.release();
    cv::destroyAllWindows();
    for(auto &thread : threads)
        delete thread;
    return 0;
}