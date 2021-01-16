#include <opencv2/core.hpp>
#include <thread>
#include "Semaphore.h"

class MatrixHelper {
public:
    Semaphore signalToThread;
    Semaphore signalToMain;
    MatrixHelper(cv::Mat& matrix, int start, int end);
    ~MatrixHelper();
    cv::Mat &frame;

    void startThread();
private:
    static volatile int idCounter;
    void threadMain(const std::stop_token& cancellationToken);
    int id, start, end;
    std::jthread thread;
    cv::Mat deltaFrame;
};