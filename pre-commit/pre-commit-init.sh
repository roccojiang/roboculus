#!/usr/bin/env bash

# Run this script with (sh pre-commit/pre-commit-init.sh)
# from root to install it locally on your device.
# Assumes you have pre-commit framework istalled on your computer.
echo "install pre-commit hook"
pre-commit install
echo "install commit-msg hook"
pre-commit install --hook-type commit-msg
echo "makes commit-msg-check an executable"
chmod +x pre-commit/commit-msg-check.sh
