FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-base
COPY ./build.ps1 /robin/
WORKDIR /robin

FROM build-base AS abstraction
COPY ./Robin.Abstractions/*.csproj /robin/Robin.Abstractions/
RUN pwsh -Command ". ./build.ps1; Restore-Abstraction"
COPY ./Robin.Abstractions /robin/Robin.Abstractions
RUN pwsh -Command ". ./build.ps1; Build-Abstraction"

FROM abstraction AS build-impl
COPY ./Implementations/*/*.csproj /robin/Implementations/
RUN for file in $(ls /robin/Implementations/*.csproj); do \
    mkdir -p Implementations/$(basename ${file%.*}) && mv $file Implementations/$(basename ${file%.*})/; \
done
RUN pwsh -Command ". ./build.ps1; Restore-Implementations"
COPY ./Implementations /robin/Implementations
RUN pwsh -Command ". ./build.ps1; Publish-Implementations"

FROM abstraction AS build-mid
COPY ./Middlewares/*/*.csproj /robin/Middlewares/
RUN for file in $(ls /robin/Middlewares/*.csproj); do \
    mkdir -p Middlewares/$(basename ${file%.*}) && mv $file Middlewares/$(basename ${file%.*})/; \
done
RUN pwsh -Command ". ./build.ps1; Restore-Middlewares"
COPY ./Middlewares /robin/Middlewares
RUN pwsh -Command ". ./build.ps1; Publish-Middlewares"

FROM build-mid AS build-ext
COPY ./Extensions/*/*.csproj /robin/Extensions/
RUN for file in $(ls /robin/Extensions/*.csproj); do \
    mkdir -p Extensions/$(basename ${file%.*}) && mv $file Extensions/$(basename ${file%.*})/; \
done
RUN pwsh -Command ". ./build.ps1; Restore-Extensions"
COPY ./Extensions /robin/Extensions
RUN pwsh -Command ". ./build.ps1; Publish-Extensions"

FROM abstraction AS build-app
COPY ./Robin.App/*.csproj /robin/Robin.App/
RUN pwsh -Command ". ./build.ps1; Restore-App"
COPY ./Robin.App /robin/Robin.App
RUN pwsh -Command ". ./build.ps1; Publish-App"

FROM build-base AS merge
COPY --from=build-app /robin/out/Robin.App /robin/out/Robin.App
COPY --from=build-impl /robin/out/Implementations /robin/out/Implementations
RUN pwsh -Command ". ./build.ps1; Remove-TransitiveDependencies -DirName Implementations"
COPY --from=build-mid /robin/out/Middlewares /robin/out/Middlewares
RUN pwsh -Command ". ./build.ps1; Remove-TransitiveDependencies -DirName Middlewares"
COPY --from=build-ext /robin/out/Extensions /robin/out/Extensions
RUN pwsh -Command ". ./build.ps1; Remove-TransitiveDependencies -DirName Extensions"
RUN pwsh -Command ". ./build.ps1; Build-FinalStructure"

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
RUN apt-get update && apt-get install -y fontconfig
COPY --from=merge /robin/out /app
WORKDIR /app/data
ENTRYPOINT ["/app/Robin.App"]
