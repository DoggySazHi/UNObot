#include <thread>
#include "Semaphore.h"

class MIDIHelper {
public:
    Semaphore signalToThread;
    Semaphore signalToMain;
    MIDIHelper();
    ~MIDIHelper();

    void startThread();
private:
    static volatile int idCounter;
    void threadMain(const std::stop_token& cancellationToken);
    int id;
    std::jthread thread;
};