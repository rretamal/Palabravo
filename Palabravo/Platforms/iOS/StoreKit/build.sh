#!/bin/sh
set -eu
out="$1"
rid="$2"
case "$rid" in
  ios-arm64) sdk=iphoneos; target=arm64-apple-ios15.0 ;;
  iossimulator-arm64) sdk=iphonesimulator; target=arm64-apple-ios15.0-simulator ;;
  iossimulator-x64) sdk=iphonesimulator; target=x86_64-apple-ios15.0-simulator ;;
  *) echo "Unsupported StoreKit runtime: $rid" >&2; exit 1 ;;
esac
mkdir -p "$out"
xcrun --sdk "$sdk" swiftc -parse-as-library -O -emit-library -static \
  -sdk "$(xcrun --sdk "$sdk" --show-sdk-path)" -target "$target" \
  -module-name PalabravoStore "$(dirname "$0")/PalabravoStore.swift" \
  -o "$out/libPalabravoStore.a"
