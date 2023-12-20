#!/usr/bin/env bash

dotnet workload update
dotnet workload install aspire

dotnet build ./build/Build.proj