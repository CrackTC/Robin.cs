FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS abstraction
WORKDIR /robin/Robin.Abstractions
COPY ./Robin.Abstractions/Robin.Abstractions.csproj ./
RUN dotnet restore
COPY ./Robin.Abstractions ./

FROM abstraction AS build-app
WORKDIR /robin/Robin.App
COPY ./Robin.App/Robin.App.csproj ./
RUN dotnet restore
COPY ./Robin.App ./
RUN dotnet publish -c Release -o /out

FROM abstraction AS build-impl
WORKDIR /robin/Implementations
COPY ./Implementations ./
RUN find . -maxdepth 1 -type d -exec sh -c 'cd {} && dotnet publish -c Release -o /out/Implementations/{}' \;

FROM abstraction AS build-ext
WORKDIR /robin/Extensions
COPY ./Extensions ./
RUN find . -maxdepth 1 -type d -exec sh -c 'cd {} && dotnet publish -c Release -o /out/Extensions/{}' \;

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS final
WORKDIR /app
COPY --from=build-app /out .
COPY --from=build-impl /out/Implementations ./Implementations
COPY --from=build-ext /out/Extensions ./Extensions
WORKDIR /app/data
ENTRYPOINT ["dotnet", "exec", "/app/Robin.App.dll"]
