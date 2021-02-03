#include <thread>
#include "Semaphore.h"
#include "Threadable.h"

class MIDIHelper : public Threadable {
    public:
        explicit MIDIHelper(std::string& fileName);
        ~MIDIHelper() override;

        void startThread();
    private:
        static volatile int idCounter;
        void threadMain(const std::stop_token& cancellationToken);
        int id;
        std::jthread thread;
        std::string& fileName;
};