#!/bin/bash

for i in {2..50} 
do
  echo `curl http://localhost:8080/api/values -k -s ` ' ' &
done
