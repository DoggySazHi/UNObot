#include <opencv2/core.hpp>
#include <thread>
#include "Semaphore.h"
#include "Threadable.h"

class MatrixHelper : public Threadable {
public:
    MatrixHelper(cv::Mat& matrix, int start, int end);
    ~MatrixHelper() override;
    cv::Mat &frame;

    void startThread();
private:
    static volatile int idCounter;
    void threadMain(const std::stop_token& cancellationToken);
    int id, start, end;
    std::jthread thread;
    cv::Mat deltaFrame;
};