cmake_minimum_required(VERSION 3.24)
project(MisaImageWorker LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(misa_image_worker STATIC src/image_worker.cpp)
target_include_directories(misa_image_worker PUBLIC include)
target_compile_features(misa_image_worker PUBLIC cxx_std_20)

if(MSVC)
  target_compile_options(misa_image_worker PRIVATE /W4 /permissive-)
else()
  target_compile_options(misa_image_worker PRIVATE -Wall -Wextra -Wpedantic)
endif()

add_executable(misa_image_worker_smoke tests/smoke.cpp)
target_link_libraries(misa_image_worker_smoke PRIVATE misa_image_worker)
enable_testing()
add_test(NAME image_worker_smoke COMMAND misa_image_worker_smoke)
