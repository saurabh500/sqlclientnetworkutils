#!/bin/bash

for i in {1..100} 
do
  echo `curl http://localhost:8080/api/values -k -s ` ' ' &
done
