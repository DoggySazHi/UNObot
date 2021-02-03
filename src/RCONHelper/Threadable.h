#include "Semaphore.h"

#ifndef Threadable_DEF
#define Threadable_DEF

class Threadable {
    public:
        Semaphore signalToThread;
        Semaphore signalToMain;
        virtual ~Threadable() = default;
};

#endif