from conans import ConanFile, CMake

class SharpDetectProfilerConan(ConanFile):
    settings = "os", "compiler", "build_type", "arch"
    requires = "cppzmq/4.8.1", "protobuf/3.20.0", "libsodium/cci.20220430"
    generators = "cmake", "cmake_paths", "cmake_find_package"

def imports(self):
    self.copy("*.dll", dst="bin", src="bin")
    self.copy("*.so*", dst="bin", src="lib")

def build(self):
	cmake = CMake(self)
	cmake.configure()
	cmake.build()