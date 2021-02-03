#include "WoolData.h"
#include "RCONHelper.h"
#include "MatrixHelper.h"
#include "MIDIHelper.h"
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
    // If you're not using WSL with Xwin redirect, comment this out.
    setenv("DISPLAY", ":1", true);
    //setenv("DISPLAY", "localhost:10.0", true);
    std::cout << "Reading from " << std::filesystem::current_path() << '\n';
    cv::Mat video;
    cv::Mat frame;
    cv::VideoCapture cap("/tmp/tmp.FlC7Zw1mO9/bad_apple_pv.mp4");

    if(!cap.isOpened()) {
        std::cout << "Error opening file!" << '\n';
        return 1;
    }

    std::vector<Threadable*> threads;

    auto frameRate = (int) cap.get(cv::CAP_PROP_FPS);
    long framesElapsed, frameCount = 0;
    auto lastTime = std::chrono::steady_clock::now();
    auto startTime = std::chrono::steady_clock::now();
    float fps = 0.0;
    while (true) {
        auto timeDiff = std::chrono::duration_cast<std::chrono::duration<float, std::milli>>(std::chrono::steady_clock::now() - startTime);
        auto targetFrames = (int) (timeDiff.count() / 1000.0f * (float) frameRate);

        if (framesElapsed < targetFrames) {
            // If running too slow
            while (framesElapsed < targetFrames) {
                cap.grab();
                framesElapsed++;
            }
        } else {
            // If running too fast
            auto ms = (int) ((float) (framesElapsed - targetFrames) / frameRate * 1000.0f);
            //if (ms < 1000) // to prevent infinite sleeping
                std::this_thread::sleep_for(std::chrono::milliseconds(ms));
        }

        cap >> video;
        if (video.empty())
            break;
        float factor = 160.0f / (float) video.cols;
        cv::resize(video, frame, cv::Size(), factor, factor);
        cv::putText(video, std::to_string(frame.cols) + " x " + std::to_string(frame.rows), cv::Point(video.cols - 50, video.rows - 20), cv::FONT_HERSHEY_DUPLEX, 0.4, cv::Scalar(0, 0, 255), 1);
        cv::putText(video, "FPS: " + std::to_string(fps), cv::Point(video.cols - 75, video.rows - 5), cv::FONT_HERSHEY_DUPLEX, 0.4, cv::Scalar(0, 0, 255), 1);
        cv::imshow("RCON Video Preview", video);
        if (cv::waitKey(1) == 27) // esc
            break;
        if (threads.empty()) {
            const int THREAD_COUNT = 150;
            auto delta = (int) ceil((float) frame.rows / THREAD_COUNT);
            for(int i = 0; i < THREAD_COUNT; i++) {
                if ((i + 1) * delta > frame.rows) {
                    auto temp = new MatrixHelper(frame, i * delta, frame.rows);
                    threads.push_back(temp);
                    break;
                }
                auto temp = new MatrixHelper(frame, i * delta, (i + 1) * delta);
                threads.push_back(temp);
            }
            auto midiFile = std::string("/tmp/tmp.FlC7Zw1mO9/nomico_bad_apple_decode.mid");
            threads.push_back(new MIDIHelper(midiFile));
            std::cout << "Spawned " << THREAD_COUNT << " Video + 1 Audio processing threads!\n";
            startTime = std::chrono::steady_clock::now(); // Set time correctly!
        }

        for(auto &thread : threads)
            thread->signalToThread.notify();
        for(auto &thread : threads)
            thread->signalToMain.wait();
        frameCount++;
        framesElapsed++;

        auto fpsDiffDuration = std::chrono::duration_cast<std::chrono::duration<float, std::milli>>(std::chrono::steady_clock::now() - lastTime);
        auto fpsDiff = fpsDiffDuration.count();
        if (fpsDiff >= 1000) {
            fps = (float) frameCount / fpsDiff * 1000.0f;
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