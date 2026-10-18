netstat -antlp|grep ____服务的Web端口____
ps -ef |grep ____服务完整名称____|awk '{print $2}'|xargs kill -9
export DOTNET_ROOT=/opt/soft/.dotnet
export PATH=$PATH:/opt/soft/.dotnet
nohup dotnet ____服务完整名称____.dll >/dev/null 2>&1 &
echo "服务已启动"