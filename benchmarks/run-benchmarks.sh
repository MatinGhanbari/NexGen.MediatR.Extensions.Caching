#!/usr/bin/env bash
set -euo pipefail

SUITE="${1:-all}"
SKIP_DOCKER="${SKIP_DOCKER:-0}"
KEEP_CONTAINERS="${KEEP_CONTAINERS:-0}"

BENCHMARKS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$BENCHMARKS_DIR/docker-compose.yml"
PROJECT_NAME="nexgen-benchmarks"
STARTED_COMPOSE=0

tcp_ready() {
  local host="$1"
  local port="$2"
  timeout 1 bash -c "echo > /dev/tcp/${host}/${port}" >/dev/null 2>&1
}

wait_tcp() {
  local host="$1"
  local port="$2"
  local seconds="${3:-60}"
  local i
  for ((i = 0; i < seconds; i++)); do
    if tcp_ready "$host" "$port"; then
      return 0
    fi
    sleep 1
  done
  return 1
}

cleanup() {
  if [[ "$STARTED_COMPOSE" -eq 1 && "$KEEP_CONTAINERS" != "1" ]]; then
    echo "Stopping benchmark containers..."
    docker compose -p "$PROJECT_NAME" -f "$COMPOSE_FILE" down
  fi
}
trap cleanup EXIT

needs_docker=0
if [[ "$SKIP_DOCKER" != "1" && ( "$SUITE" == "all" || "$SUITE" == "provider" ) ]]; then
  needs_docker=1
fi

if [[ "$needs_docker" -eq 1 ]]; then
  if tcp_ready 127.0.0.1 6379 && tcp_ready 127.0.0.1 6380; then
    echo "Redis and Garnet already reachable; skipping docker compose up."
  else
    echo "Starting Redis (6379) and Garnet (6380) via docker compose..."
    docker compose -p "$PROJECT_NAME" -f "$COMPOSE_FILE" up -d
    STARTED_COMPOSE=1
  fi

  wait_tcp 127.0.0.1 6379 || { echo "Timed out waiting for Redis on localhost:6379" >&2; exit 1; }
  wait_tcp 127.0.0.1 6380 || { echo "Timed out waiting for Garnet on localhost:6380" >&2; exit 1; }
  echo "Redis and Garnet are ready."
fi

PROJECT="$BENCHMARKS_DIR/NexGen.MediatR.Extensions.Caching.Benchmark/NexGen.MediatR.Extensions.Caching.Benchmark.csproj"
echo "Running benchmark suite '$SUITE'..."
dotnet run -c Release --project "$PROJECT" --no-launch-profile -- "$SUITE"
