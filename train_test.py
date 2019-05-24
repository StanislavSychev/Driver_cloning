import torch.nn as nn
import torch.optim as optim
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import random
from r_score import R2Score
from data_reader import read_all_trajectories
from sklearn.neighbors import NearestNeighbors
from os import listdir
from model import KNNDriver, get_kn, load_model

if __name__ == '__main__':
    dreivers = list(set([s[:-2] for s in listdir("parsedData")]))
    random.shuffle(dreivers)
    s = len(dreivers) // 5
    train = dreivers[s+1:]
    test = dreivers[:s]

    qest = pd.read_csv("preQuestionnaire.csv", index_col=0)
    qest = qest.fillna(0)
    qest = qest.replace({'gender': r'^[f|F].*'}, {'gender': 1}, regex=True)
    qest = qest.replace({'gender': r'^[m|M].*'}, {'gender': 2}, regex=True)
    qest = qest.replace({'gender': r'^h.*'}, {'gender': 0}, regex=True)

    absent = []
    for i in qest.ID:
        if str(i) not in dreivers:
            absent.append(i)

    for d in absent:
        qest = qest[qest.ID != d]

    tq = qest

    for d in test:
        tq = tq[tq.ID != float(d)]

    print(tq)
    print(qest)

    def get_by_id(driver_id):
        row = qest[qest.ID == driver_id]
        row = row.drop(columns=['ID'])
        return row.values

    s, a = read_all_trajectories("parsedData", 0)
    score = R2Score()
    testm = [load_model("models/{}A{}.pt".format(d, 0)) for d in test]
    rs = []
    nk = []
    for k in range(2, 11):
        print(k)
        r = 0
        for d, dm in zip(test, testm):
            p = get_by_id(float(d))
            m = KNNDriver(get_kn(p, k, tq), 0)
            r += score(m(s), dm(s)).item()
            print(r)
        r = r / len(test)
        rs.append(r)
        nk.append(k)
    plt.plot(nk, rs)
    plt.show()


