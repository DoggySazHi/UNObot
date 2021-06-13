#include "MatrixHelper.h"
#include "WoolData.h"
#include "RCONHelper.h"
#include <iostream>

volatile int MatrixHelper::idCounter = 0;

MatrixHelper::MatrixHelper(cv::Mat &matrix, int start, int end) : id(0), frame(matrix), start(start), end(end) {
    startThread();
}

MatrixHelper::~MatrixHelper() {
    thread.request_stop();
    signalToThread.notify();
    std::cout << "Destroying thread #" << id << "!\n";
}

void MatrixHelper::startThread() {
    thread = std::jthread([this](const std::stop_token& cancellationToken){
        id = idCounter;
        idCounter = idCounter + 1;
        std::cout << "Started thread #" << id << "!\n";
        MatrixHelper::threadMain(cancellationToken);
    });
}

void MatrixHelper::threadMain(const std::stop_token& cancellationToken) {
    WoolData woolData;
    const char* ip = "127.0.0.1";
    unsigned short port = 25575;
    const char* password = "mukyumukyu";
    auto rcon = RCONHelper::CreateObjectB(ip, port, password);
    while (!cancellationToken.stop_requested()){
        signalToThread.wait(cancellationToken);
        if (cancellationToken.stop_requested())
            break;
        if(deltaFrame.rows != end - start || deltaFrame.cols != frame.cols)
            deltaFrame = cv::Mat(end - start, frame.cols, CV_8UC1, 69);

        for(int r = start; r < end; r++) {
            const unsigned char* row = frame.ptr<unsigned char>(r);
            for (int c = 0; c < frame.cols; c++) {
                auto posDelta = deltaFrame.step1() * (r - start) + c;
                auto colorR = row[c * 3 + 2]; // OpenCV is BGR, thanks Python
                auto colorG = row[c * 3 + 1];
                auto colorB = row[c * 3];
                auto colorSheep = WoolData::getClosestColor((int) colorR, (int) colorG, (int) colorB);
                if (deltaFrame.data[posDelta] != colorSheep->id) {
                    deltaFrame.data[posDelta] = colorSheep->id;
                    auto command = woolData.getPayload(r, c, colorSheep);
                    rcon->ExecuteFast(command);
                }
            }
        }
        signalToMain.notify();
    }

    signalToThread.dispose();
    signalToMain.dispose();
    RCONHelper::SayDelete(reinterpret_cast<const char *>(rcon));
    std::cout << "Ended thread #" << id << "!\n";
}
