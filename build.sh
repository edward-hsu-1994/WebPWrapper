set -e

# Read Version
version=$(cat version)

# Output Path
path=$(pwd)
path="$path/ngpkgs"

# Remove Old Output
rm -R -f $path

dotnet restore WebPWrapper

dotnet test WebPWrapper.Test

dotnet build WebPWrapper

dotnet pack WebPWrapper -p:Version=$version --output $path; 
