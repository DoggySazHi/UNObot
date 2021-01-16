#include <mutex>
#include <condition_variable>

class Semaphore {
public:
    explicit Semaphore(int count_ = 0) : count(count_) {}

    inline void notify() {
        std::unique_lock<std::mutex> lock(mutex);
        count = count + 1;
        trigger.notify_one();
    }

    inline void wait(const std::stop_token& cancellationToken) {
        std::unique_lock<std::mutex> lock(mutex);

        while(count == 0 && !cancellationToken.stop_requested()){
            trigger.wait(lock);
        }
        count = count - 1;
    }

    inline void wait() {
        std::unique_lock<std::mutex> lock(mutex);

        while(count == 0){
            trigger.wait(lock);
        }
        count = count - 1;
    }

private:
    mutable std::mutex mutex;
    std::condition_variable trigger;
    volatile int count;
};