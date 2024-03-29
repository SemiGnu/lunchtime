﻿build:
	docker  build -t semignu/lunchtime:dev --file .\LunchTime.Api\Dockerfile .
 
run:
	docker run --rm --name lunchtime-dev -p 8080:8080 --env-file .\.env semignu/lunchtime:dev

push:
	docker buildx build --builder default -t semignu/lunchtime:latest -t semignu/lunchtime:amd64 --push --file .\LunchTime.Api\Dockerfile .

push2:
	docker buildx build --platform linux/amd64 -t semignu/lunchtime:latest -t semignu/lunchtime:amd64 --push --file .\LunchTime.Api\Dockerfile .
	docker buildx build --platform linux/arm64 -t semignu/lunchtime:arm64 --push --file .\LunchTime.Api\Dockerfile .
