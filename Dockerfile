FROM ubuntu:24.04

RUN apt-get update -y && apt-get update -y && apt-get install -y --no-install-recommends \
    build-essential \
	zlib1g-dev \
    git \
	cmake \
	clang-18 \
	dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/*