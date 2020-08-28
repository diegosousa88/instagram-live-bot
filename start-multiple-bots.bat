@echo off

cd %~d0
start cmd.exe /c "C:\Program Files\LINQPad5\LPRun.exe" InstagramLiveRobot.linq hater [BaseAccountId] [Password]
ping 192.168.0.30 -n 1 -w 60000 > nul
start cmd.exe /c "C:\Program Files\LINQPad5\LPRun.exe" InstagramLiveRobot.linq follower [BaseAccountId] [Password]