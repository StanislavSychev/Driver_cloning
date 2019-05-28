import torch
import torch.nn as nn
import torch.optim as optim
import matplotlib.pyplot as plt
from r_score import R2Score
from data_reader import read_all, read_driver_trajectories, read_all_trajectories
from model import DriverControls, LinearConvDriver, load_model, LinearConvQestDriver, LinearConvQestDriver1
from os import listdir
from os.path import exists


def train_action(model, action_type):
    state, action = read_all("parsedData", action_type)
    # state, action = read_all_trajectories("parsedData", action_type)
    state = state.view(state.size(0), 1, -1)
    loss_function = nn.MSELoss()
    score = R2Score()
    optimizer = optim.Adam(model.parameters(), lr=0.001)
    err = []
    max_epoch = 1000
    for epoch in range(max_epoch):
        model.zero_grad()
        got = model(state)
        loss = loss_function(got, action)
        err.append(score(got, action).item())
        print("{}, {}: {}".format(action_type, epoch, err[-1]))
        if epoch == max_epoch - 1 or epoch == 0:
            print(got)
        loss.backward()
        optimizer.step()
    print(action)
    print(score(model(state), action).item())
    fig, ax = plt.subplots()
    ax.plot(err, 'k')
    ax.set(xlabel='epoch', ylabel='R2-score',
           title='Learning curve for action {}'.format(action_type))
    fig.savefig("action{}.png".format(action_type))
    plt.show()
    torch.save(model, "Action{}_1.pt".format(action_type))


def test_action(model, action_type):
    state, action = read_all("test", action_type)
    state = state.view(state.size(0), 1, -1)
    score = R2Score()
    got = model(state)
    print(score(got, action))


def train_driver(driver, action_type):
    model = LinearConvDriver(100, 4, 4, 100, 5, 1)
    if driver == '888':
        return
    state, action = read_driver_trajectories("parsedData", driver, action_type)
    state = state.view(state.size(0), 1, -1)
    loss_function = nn.MSELoss()
    score = R2Score()
    optimizer = optim.Adam(model.parameters())
    err = []
    max_epoch = 2000
    for epoch in range(max_epoch):
        model.zero_grad()
        got = model(state)
        loss = loss_function(got, action)
        err.append(score(got, action).item())
        print("{}: {}".format(epoch, err[-1]))
        if epoch == max_epoch - 1 or epoch == 0:
            print(got)
        loss.backward()
        optimizer.step()

    # print(action)
    # print(score(model(state), action).item())
    # fig, ax = plt.subplots()
    # ax.plot(err, 'k')
    # ax.set(xlabel='epoch', ylabel='R2-score',
    #        title='Learning curve for action {}'.format(action_type))
    # fig.savefig("action{}.png".format(action_type))
    # plt.show()
    torch.save(model, "models/{}A{}.pt".format(driver, action_type))


def train_action_models(action):
    dreivers = set([s[:-2] for s in listdir("parsedData")])
    print(dreivers)
    for d in dreivers:
        print(d)
        if not exists("models/{}A{}.pt".format(d, action)):
            train_driver(d, action)


if __name__ == '__main__':
    # for i in range(1, 3):
    #     model = LinearConvQestDriver1(100, 4, 4, 22, 3, 200, 5, 1)
    #     # model = DriverControls(100, 5, 4, 4, 200, 10, 22, 22, 1)
    #     # model = load_model("Action3_1.pt")
    #     train_action(model, i)
    train_action_models(3)
    # model = LinearConvDriver(100, 4, 4, 200, 5, 1)
    # train_action_models(1)
    # train_action_models(2)
    # train_driver("18.1", 0)
