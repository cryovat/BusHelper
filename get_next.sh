#!/bin/bash
cat $1 | gawk -F\| "{if (systime()<\$1){print \$0; exit;}}" | sort -t\| -nk2
