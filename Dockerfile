# Use the official .NET image as a base image for building the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory inside the container
WORKDIR /app

# Copy everything first (including your nuget.config with placeholder)
COPY . ./

# Replace placeholder token in nuget.config with actual PAT from build arg
ARG GITHUB_PACKAGE_PAT
RUN if [ -n "$GITHUB_PACKAGE_PAT" ]; then \
      sed -i "s|__GITHUB_PACKAGE_PAT__|$GITHUB_PACKAGE_PAT|g" ./nuget.config; \
    fi

# Restore dependencies (now with real token in nuget.config)
RUN dotnet restore

# Build the application in Release configuration and output to the /out directory
RUN dotnet publish -c Release -o out

# Use a runtime-only .NET image for the final image (smaller size)
FROM mcr.microsoft.com/dotnet/runtime:9.0

# Install necessary Linux packages (for fonts and graphical capabilities)
RUN apt-get update && \
    apt-get install -y libharfbuzz0b libfontconfig1 libfreetype6 libgl1-mesa-dev libglu1-mesa && \
    rm -rf /var/lib/apt/lists/*

# Set the working directory inside the final container
WORKDIR /app

# Copy the wordlist and answerlist files into the container (ensure paths are correct)
COPY ./commands/wordle-group/helpers/wordList.txt /app/commands/wordle-group/helpers/wordList.txt
COPY ./commands/wordle-group/helpers/answerList.txt /app/commands/wordle-group/helpers/answerList.txt

# Copy the output of the build from the previous stage
COPY --from=build /app/out .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "main.dll"]
