import time
import zmq
import pandas as pd
from player_model import Player


def update(state):
    return b"OK"


def action():
    return b"0.5;0.5;0" == bytes("{};{};{}".format(0.5, 0.5, 0))


if __name__ == "__main__":
    context = zmq.Context()
    socket = context.socket(zmq.REP)
    socket.bind("tcp://*:5555")
    qest = pd.read_csv("preQuestionnaire.csv", index_col=0)
    qest = qest.fillna(0)
    qest = qest.replace({'gender': r'^[f|F].*'}, {'gender': 1}, regex=True)
    qest = qest.replace({'gender': r'^[m|M].*'}, {'gender': 2}, regex=True)
    qest = qest.replace({'gender': r'^h.*'}, {'gender': 0}, regex=True)
    p = list(qest.loc[32])[1:]
    player = Player(k=1, player_type=0,
                    p=p)
    while True:
        message = socket.recv()
        request = str(message)[2:-2]
        if request[:3] == "stt":
            player.update(request[3:])
            socket.send(b"OK")
        else:
            socket.send(player.act())
