FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS abstraction
WORKDIR /robin/Robin.Abstractions
COPY ./Robin.Abstractions/Robin.Abstractions.csproj ./
RUN dotnet restore
COPY ./Robin.Abstractions ./
RUN dotnet build -c Release

FROM abstraction AS build-impl
WORKDIR /robin/Implementations
COPY ./Implementations ./
RUN find . ! -path . -maxdepth 1 -type d -exec sh -c 'cd {} && dotnet publish -c Release -o /out/Implementations/{}' \;

FROM abstraction AS build-mid
WORKDIR /robin/Middlewares
COPY ./Middlewares ./
RUN find . ! -path . -maxdepth 1 -type d -exec sh -c 'cd {} && dotnet publish -c Release -o /out/Middlewares/{}' \;

FROM build-mid AS build-ext
WORKDIR /robin/Extensions
COPY ./Extensions ./
RUN find . ! -path . -maxdepth 1 -type d -exec sh -c 'cd {} && dotnet publish -c Release -o /out/Extensions/{}' \;

FROM build-mid AS build-app
WORKDIR /robin/Robin.App
COPY ./Robin.App/Robin.App.csproj ./
RUN dotnet restore
COPY ./Robin.App ./
RUN dotnet publish -c Release -o /out/Robin.App

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS final
RUN apk add --no-cache icu-libs
WORKDIR /app
COPY --from=build-app /out/Robin.App .
COPY --from=build-impl /out/Implementations ./Implementations
COPY --from=build-ext /out/Extensions ./Extensions
RUN for f in $(ls *.dll); do rm -f /app/Extensions/*/$(basename $f); done
WORKDIR /app/data
ENTRYPOINT ["dotnet", "exec", "/app/Robin.App.dll"]
