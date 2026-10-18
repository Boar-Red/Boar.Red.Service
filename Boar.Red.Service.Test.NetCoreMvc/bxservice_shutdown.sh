
netstat -antlp|grep ____服务的Web端口____
ps -ef |grep ____服务完整名称____|awk '{print $2}'|xargs kill -9
echo 服务已关机