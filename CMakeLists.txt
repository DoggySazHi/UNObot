cmake_minimum_required (VERSION 3.8)

project ("RCONHelper")

set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)

file(GLOB RCONHelper_SRC
        "src/RCONHelper/*.h"
        "src/RCONHelper/*.cpp"
        "src/RCONHelper/*.c"
        )

add_library(RCONHelper SHARED ${RCONHelper_SRC})
add_executable(RCONHelper_Test ${RCONHelper_SRC})

if(MSVC)
    target_compile_options(RCONHelper PRIVATE /W4 /WX)
else()
    target_compile_options(RCONHelper PRIVATE -Wall -Wextra -pedantic -Werror)
endif()


set_property(TARGET RCONHelper PROPERTY CXX_STANDARD 20)
set_property(TARGET RCONHelper PROPERTY CXX_STANDARD_REQUIRED 17)
set_property(TARGET RCONHelper_Test PROPERTY CXX_STANDARD 20)
set_property(TARGET RCONHelper_Test PROPERTY CXX_STANDARD_REQUIRED 17)