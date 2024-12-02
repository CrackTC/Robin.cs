FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS abstraction
WORKDIR /robin/Robin.Abstractions
COPY ./Robin.Abstractions/Robin.Abstractions.csproj ./
RUN dotnet restore
COPY ./Robin.Abstractions ./
RUN dotnet build -c Release

FROM abstraction AS build-impl
WORKDIR /robin/Implementations
COPY ./Implementations ./
RUN for impl in */; do \
    cd $impl; \
    dotnet publish -c Release -o /out/Implementations/$impl || exit 1; \
    cd -; \
done

FROM abstraction AS build-mid
WORKDIR /robin/Middlewares
COPY ./Middlewares ./
RUN for mid in */; do \
    cd $mid; \
    dotnet publish -c Release -o /out/Middlewares/$mid || exit 1; \
    cd -; \
done

FROM build-mid AS build-ext
WORKDIR /robin/Extensions
COPY ./Extensions ./
RUN for ext in */; do \
    cd $ext; \
    dotnet publish -c Release -o /out/Extensions/$ext || exit 1; \
    cd -; \
done

FROM build-mid AS build-app
WORKDIR /robin/Robin.App
COPY ./Robin.App/Robin.App.csproj ./
RUN dotnet restore
COPY ./Robin.App ./
RUN dotnet publish -c Release -o /out/Robin.App

FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS final
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apk add --no-cache icu-libs freetype
WORKDIR /app
COPY --from=build-app /out/Robin.App .
COPY --from=build-impl /out/Implementations ./Implementations
COPY --from=build-ext /out/Extensions ./Extensions
RUN for f in $(ls *.dll); do rm -f /app/Extensions/*/$(basename $f); done
WORKDIR /app/data
ENTRYPOINT ["dotnet", "exec", "/app/Robin.App.dll"]
