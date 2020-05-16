cmake_minimum_required (VERSION 3.8)

project ("RCONHelper")

file(GLOB RCONHelper_SRC
        "src/RCONHelper/*.h"
        "src/RCONHelper/*.cpp"
        "src/RCONHelper/*.c"
        )

add_library(RCONHelper SHARED ${RCONHelper_SRC})