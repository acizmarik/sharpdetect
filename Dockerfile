FROM ubuntu:24.04

ARG BUILD_CONFIGURATION="Debug"
ARG NET_SDK_VERSION="net8.0"

ENV BUILD_CONFIGURATION=$BUILD_CONFIGURATION
ENV NET_SDK_VERSION=$NET_SDK_VERSION

RUN apt-get update -y && apt-get update -y && apt-get install -y --no-install-recommends \
	# Dependencies
	build-essential \
	zlib1g-dev \
	git \
	cmake \
	clang-18 \
	dotnet-sdk-8.0 \
	&& rm -rf /var/lib/apt/lists/*

RUN git clone https://github.com/acizmarik/sharpdetect.git && \
	cd sharpdetect/src && \
	# Build
	git submodule update --init --recursive && \
	dotnet tool restore && \
	dotnet cake --rid=linux-x64 && \
	# Installation / prepare workspace
	cd / && \
	mv sharpdetect/ sharpdetect-tmp/ && \
	mkdir -p sharpdetect/Cli sharpdetect/Plugins sharpdetect/Runtime && \
	cp -r sharpdetect-tmp/src/SharpDetect.Cli/bin/$BUILD_CONFIGURATION/$NET_SDK_VERSION/* sharpdetect/Cli/ && \
	cp -r sharpdetect-tmp/src/Extensibility/SharpDetect.Plugins/bin/$BUILD_CONFIGURATION/$NET_SDK_VERSION/* sharpdetect/Plugins/ && \
	cp -r sharpdetect-tmp/src/artifacts/* sharpdetect/Runtime/ && \
	rm -rf sharpdetect-tmp