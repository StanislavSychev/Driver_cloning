import torch
import torch.nn as nn
import torch.optim as optim
import matplotlib.pyplot as plt
from r_score import R2Score
from data_reader import read_all
from model import DriverControls


def train_action(model, action_type):
    state, action = read_all("parsedData", action_type)
    state = state.view(state.size(0), 1, -1)
    loss_function = nn.MSELoss()
    score = R2Score()
    optimizer = optim.Adam(model.parameters())
    err = []
    max_epoch = 1
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
    print(action)
    print(score(model(state), action).item())
    fig, ax = plt.subplots()
    ax.plot(err, 'k')
    ax.set(xlabel='epoch', ylabel='R2-score',
           title='Learning curve for action {}'.format(action_type))
    fig.savefig("action{}.png".format(action_type))
    plt.show()
    torch.save(model, "Action{}.pt".format(action_type))


def test_action(model, action_type):
    state, action = read_all("test", action_type)
    state = state.view(state.size(0), 1, -1)
    score = R2Score()
    got = model(state)
    print(score(got, action))


if __name__ == '__main__':
    model = DriverControls(50, 4, 4, 100, 1, 22, 5, 1)
    train_action(model, 0)
