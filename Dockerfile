# Use the official .NET image as a base image for building the app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Set the working directory inside the container
WORKDIR /app

# Copy the .csproj files and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application source code into the container
COPY . ./

# Build the application in Release configuration and output to the /out directory
RUN dotnet publish -c Release -o out

# Use a runtime-only .NET image for the final image (smaller size)
FROM mcr.microsoft.com/dotnet/runtime:6.0

# Install necessary Linux packages (for fonts and graphical capabilities)
RUN apt-get update && \
    apt-get install -y libharfbuzz0b libfontconfig1 libfreetype6 libgl1-mesa-dev libglu1-mesa && \
    rm -rf /var/lib/apt/lists/*

# Set the working directory inside the final container
WORKDIR /app

# Copy the wordlist and answerlist files into the container (ensure paths are correct)
# These files must exist in the build context (the directory you're running `docker build` from)
COPY ./commands/wordle-group/helpers/wordList.txt /app/commands/wordle-group/helpers/wordList.txt
COPY ./commands/wordle-group/helpers/answerList.txt /app/commands/wordle-group/helpers/answerList.txt

# Copy the output of the build from the previous stage
COPY --from=build /app/out .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "main.dll"]
