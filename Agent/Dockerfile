# Stage 1: Base Runtime Environment
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y tcpdump default-jre-headless libpcap-dev && rm -rf /var/lib/apt/lists/*

# Stage 2: Build the C# Agent
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-dotnet
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HeimdallAgent.csproj", "."]
RUN dotnet restore "./HeimdallAgent.csproj"
COPY . .
RUN dotnet build "./HeimdallAgent.csproj" -c $BUILD_CONFIGURATION -o /app/build
RUN dotnet publish "./HeimdallAgent.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 3: Build CICFlowMeter from Source (Using Legacy Java 8)
FROM ubuntu:22.04 AS build-java

RUN apt-get update && \
    apt-get install -y openjdk-8-jdk git maven dos2unix unzip && \
    rm -rf /var/lib/apt/lists/*

ENV JAVA_HOME=/usr/lib/jvm/java-8-openjdk-amd64
ENV PATH=$JAVA_HOME/bin:$PATH

WORKDIR /src
RUN git clone https://github.com/ahlashkari/CICFlowMeter.git .

# 1. Install the local jnetpcap dependency via Maven
RUN mvn install:install-file -Dfile=jnetpcap/linux/jnetpcap-1.4.r1425/jnetpcap.jar -DgroupId=org.jnetpcap -DartifactId=jnetpcap -Dversion=1.4.1 -Dpackaging=jar

# 2. Fix line endings, disable the daemon, and compile
RUN dos2unix gradlew && chmod +x gradlew
RUN ./gradlew distZip --no-daemon

# 3. Foolproof Extraction & Staging (Bypassing unzip security heuristics)
RUN mkdir -p /app-stage /tmp/extract && \
    cp /src/build/distributions/*.zip /tmp/app.zip && \
    cd /tmp/extract && \
    jar xf /tmp/app.zip && \
    cd / && \
    mv /tmp/extract/*/* /app-stage/

# Stage 4: Final Assembly
FROM base AS final
WORKDIR /app

# Copy the compiled C# Agent
COPY --from=build-dotnet /app/publish .

# Copy the staged Java application cleanly
RUN mkdir /app/CICFlowMeter
COPY --from=build-java /app-stage /app/CICFlowMeter

# Copy the native Linux jnetpcap library (.so) so Java can access eth0
COPY --from=build-java /src/jnetpcap/linux/jnetpcap-1.4.r1425/libjnetpcap.so /usr/lib/

ENTRYPOINT ["dotnet", "HeimdallAgent.dll"]