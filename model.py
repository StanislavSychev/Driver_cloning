import torch
import torch.nn as nn
import pandas as pd
from sklearn.neighbors import NearestNeighbors
import numpy as np
from os import listdir, path


class Driver(nn.Module):

    def __init__(self, input_dim, hiden_dim, n, output_dim):
        super(Driver, self).__init__()
        self.hiden_dim = hiden_dim
        self.lstm = nn.LSTM(input_dim, hiden_dim, n)
        # self.hiden = [nn.Linear(hiden_dim, hiden_dim) for _ in range(n)]
        self.to_res = nn.Linear(hiden_dim, output_dim)

    def forward(self, state):
        # print(state.view(len(state), 1, -1))
        # lstm_out, _ = self.lstm(state.view(len(state), 1, -1))
        lstm_out, _ = self.lstm(state)
        # hiden = lstm_out.view(len(state), 1, -1).view(len(state), -1)
        hiden = lstm_out[:, -1]
        # for layer in self.hiden:
        #     hiden = layer(hiden)
        return self.to_res(hiden)


class LinearDriver(nn.Module):

    def __init__(self, input_dim, hiden_dim, n, output_dim):
        super(LinearDriver, self).__init__()
        self.input = nn.Linear(input_dim, hiden_dim)
        self.hiden = [nn.Linear(hiden_dim, hiden_dim) for _ in range(n)]
        self.output = nn.Linear(hiden_dim, output_dim)
        # self.output = nn.Conv1d(hiden_dim, output_dim, )
        # self.act = nn.Softsign()
        self.act = nn.Tanh()

    def forward(self, state):
        # print(state)
        hiden = self.act(self.input(state))
        # print(hiden)
        for h in self.hiden:
            hiden = self.act(h(hiden))
        return self.act(self.output(hiden))


class LinearConvDriver(nn.Module):

    def __init__(self, conv_number, number_of_drivers, input_size, hidden_size, n, output_dim):
        super(LinearConvDriver, self).__init__()
        self.conv1 = nn.Conv2d(1, conv_number, (number_of_drivers, 1))
        self.hidden1 = nn.Linear(conv_number * input_size, hidden_size)
        self.hidden = [nn.Linear(hidden_size, hidden_size) for _ in range(n)]
        self.res = nn.Linear(hidden_size, output_dim)
        self.act = nn.Tanh()

    def forward(self, x):
        x = x.view(x.size(0), 1, 4, 4)
        x = self.act(self.conv1(x))
        x = x.view(x.size(0), -1)
        x = self.act(self.hidden1(x))
        for h in self.hidden:
            x = self.act(h(x))
        return self.act(self.res(x))


class DriverControls(nn.Module):

    def __init__(self, conv_number1, conv_number2, number_of_drivers, input_size, hidden_size, n, qest_dim, qest_hidden, output_dim):
        super(DriverControls, self).__init__()
        self.conv1 = nn.Conv2d(1, conv_number1, (number_of_drivers, 1))
        self.convp = nn.Conv1d(1, conv_number1, qest_hidden - input_size + 1)
        self.conv2 = nn.Conv2d(conv_number1, conv_number2, (2, 1))
        self.qest = nn.Linear(qest_dim, qest_hidden)
        self.hidden1 = nn.Linear(conv_number2 * input_size, hidden_size)
        self.hidden = [nn.Linear(hidden_size, hidden_size) for _ in range(n)]
        self.res = nn.Linear(hidden_size, output_dim)
        self.act = nn.Tanh()
        self.nd = number_of_drivers
        self.np = qest_dim
        self.ins = input_size

    def forward(self, x):
        p, x = x.split(self.np, 2)
        x = x.view(x.size(0), 1, self.ins, self.ins)
        x = self.act(self.conv1(x))
        p = self.act(self.qest(p))
        p = p.view(p.size(0), 1, -1)
        p = self.act(self.convp(p))
        x = x.view(x.size(0), x.size(1), -1)
        x = torch.cat((x, p), 2)
        x = x.view(x.size(0), x.size(1), 2, self.ins)
        x = self.act(self.conv2(x))
        # p = p.view(p.size(0), -1)
        x = x.view(x.size(0), -1)
        # x = torch.cat((x, p), 1)
        x = self.act(self.hidden1(x))
        for h in self.hidden:
            x = self.act(h(x))
        return self.act(self.res(x))


def load_model(filename):
    m = torch.load(filename)
    m.eval()
    return m


def get_kn(p, k, qest):
    x = qest.drop(columns=['ID']).values
    y = qest['ID'].values
    knn = NearestNeighbors(n_neighbors=k)
    knn.fit(x, y)
    dist, ind = knn.kneighbors(p)
    # print(y[list(ind)[0]])
    return y[list(ind)[0]]


class KNNDriver(nn.Module):

    def __init__(self, drivers, action):
        super(KNNDriver, self).__init__()
        self.drivers = [load_model(path.join("models", "{}A{}.pt".format(d, action))) for d in drivers]

    def forward(self, x):
        y = []
        for d in self.drivers:
            y.append(d(x))
        y = torch.cat(tuple(y), 1)
        y = torch.mean(y, dim=1, keepdim=True)
        return y


class KNNlWrapper:

    def __init__(self, p, k):
        p = np.array(p).reshape((1, len(p)))
        qest = pd.read_csv('preQuestionnaire.csv', index_col=0)
        qest = qest.fillna(0)
        qest = qest.replace({'gender': r'^[f|F].*'}, {'gender': 1}, regex=True)
        qest = qest.replace({'gender': r'^[m|M].*'}, {'gender': 2}, regex=True)
        qest = qest.replace({'gender': r'^h.*'}, {'gender': 0}, regex=True)
        dreivers = list(set([s[:-5] for s in listdir("models")]))
        absent = []
        for i in qest.ID:
            if str(i) not in dreivers:
                absent.append(i)
        for d in absent:
            qest = qest[qest.ID != d]
        ids = get_kn(p, k, qest)
        self.act1 = KNNDriver(ids, 0)
        self.act2 = KNNDriver(ids, 1)
        self.act3 = KNNDriver(ids, 2)

    def __call__(self, x):
        x = torch.FloatTensor(x)
        x = x.view(1, 1, -1)
        act1 = self.act1(x).item()
        act2 = self.act2(x).item()
        act3 = self.act3(x).item()
        return [act1, act2, act3]


class NNWrapper:

    def __init__(self, p):
        p = torch.FloatTensor(p)
        self.p = p.view(1, 1, -1)
        self.act1 = load_model("models/Action0.pt")
        self.act2 = load_model("models/Action1.pt")
        self.act3 = load_model("models/Action2.pt")

    def __call__(self, x):
        x = torch.FloatTensor(x)
        x = x.view(1, 1, -1)
        x = torch.cat((x, self.p), 2)
        act1 = self.act1(x).item()
        act2 = self.act2(x).item()
        act3 = self.act3(x).item()
        return [act1, act2, act3]


if __name__ == '__main__':
    qest = pd.read_csv("preQuestionnaire.csv", index_col=0)
    qest = qest.fillna(0)
    qest = qest.replace({'gender': r'^[f|F].*'}, {'gender': 1}, regex=True)
    qest = qest.replace({'gender': r'^[m|M].*'}, {'gender': 2}, regex=True)
    qest = qest.replace({'gender': r'^h.*'}, {'gender': 0}, regex=True)
    print(list(qest.loc[1]))