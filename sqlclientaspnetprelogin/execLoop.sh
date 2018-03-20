#!/bin/bash

for i in {2..50} 
do     
  echo `curl https://localhost:5001/api/values -k -s ` ' ' &
done
