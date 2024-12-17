# Use the official .NET image as a base image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Set the working directory to the current directory
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application
COPY . ./

# Build the application
RUN dotnet publish -c Release -o out

# Use a runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0

# Install the necessary Linux packages
RUN apt-get update && \
    apt-get install -y libharfbuzz0b libfontconfig1 libfreetype6 libgl1-mesa-dev libglu1-mesa && \
    rm -rf /var/lib/apt/lists/*

# Set the working directory to the current directory
WORKDIR /app

# Copy the wordlist for Wordle
COPY ./commands/wordle-group/helpers/wordlist.txt /app/commands/wordle-group/helpers/wordlist.txt
COPY ./commands/wordle-group/helpers/answerlist.txt /app/commands/wordle-group/helpers/answerlist.txt

# Copy the built application from the build stage
COPY --from=build /app/out .

# Set the entry point
ENTRYPOINT ["dotnet", "main.dll"]
