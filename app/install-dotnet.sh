#!/bin/bash
set -euo pipefail

curl -L https://dot.net/v1/dotnet-install.sh | bash -e -s -- --jsonfile global.json --install-dir .dotnet
