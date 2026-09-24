cmake_minimum_required(VERSION 3.25)
project(PurrxelCore CXX C)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)

find_package(lz4 CONFIG REQUIRED)

file(GLOB_RECURSE PURRXEL_CORE_SOURCES CONFIGURE_DEPENDS
    ${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp
    ${CMAKE_CURRENT_SOURCE_DIR}/src/*.c
)

file(GLOB_RECURSE PURRXEL_CORE_HEADERS CONFIGURE_DEPENDS
    ${CMAKE_CURRENT_SOURCE_DIR}/include/*.hpp
    ${CMAKE_CURRENT_SOURCE_DIR}/include/*.h
)

add_library(PurrxelCore SHARED ${PURRXEL_CORE_SOURCES} ${PURRXEL_CORE_HEADERS})

target_include_directories(PurrxelCore PUBLIC
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
    $<INSTALL_INTERFACE:include>
)

target_include_directories(PurrxelCore PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include/purrxel
)

target_link_libraries(PurrxelCore PRIVATE lz4::lz4)

target_compile_definitions(PurrxelCore PRIVATE PURRXEL_CORE_EXPORTS)
