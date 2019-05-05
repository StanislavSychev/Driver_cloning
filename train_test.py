import torch
import torch.nn as nn
import torch.optim as optim
import simplejson
import matplotlib.pyplot as plt
from r_score import R2Score


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
        x = self.act(self.conv1(x))
        x = x.view(x.size(0), -1)
        x = self.act(self.hidden1(x))
        for h in self.hidden:
            x = self.act(h(x))
        return self.act(self.res(x))


if __name__ == '__main__':
    state = []
    action = []
    do_nothing = [0, 0, 0]
    didnt_move = True
    stop_move = None
    data = open("parsedData/18.1_2", 'r')
    for line in data.readlines():
        ent = line.split("\t")
        if ent[0] == "ACTION:":
            a = simplejson.loads(ent[1])
            if a[1] == do_nothing and didnt_move:
                state = state[:-1]
                continue
            didnt_move = False
            if a[1] == do_nothing and stop_move is None:
                stop_move = len(action)
            if a[1] != do_nothing:
                stop_move = None
            action.append(torch.FloatTensor([a[1][2]]))
            # action.append(torch.FloatTensor([a[1][0]]))
        if ent[0] == "STATE:":
            s = simplejson.loads(ent[1])
            new_state = []
            states = [[d[2][0], d[2][2], d[3][1], d[3][3]] for d in s[1]]
            self = states[0]
            states = states[1:]
            states.sort(key=lambda x: (x[0] - self[0]) ** 2 + (x[1] - self[1]) ** 2)
            states = states[:3]
            for s in states:
                for item in s:
                    self.append(item)
            # for d in s[1]:
            #     new_state.append(torch.FloatTensor([d[1], d[2][0], d[2][2], d[3][1], d[3][3]]))
            # state.append(new_state)
            state.append(torch.FloatTensor(self))
    state = state[:-1]
    if stop_move:
        action = action[:stop_move]
        state = state[:stop_move]
    print(len(action))
    print(len(state))

    out_size = 1

    # state = [torch.cat(i).view(1, len(i), -1) for i in state]
    # state = torch.cat(state).view(len(state), len(state[0][0]), -1)
    state = torch.cat(state).view(len(state), -1)
    print(state)
    action = torch.cat(action).view(len(action), out_size)
    state = state.view(state.size(0), 1, 4, 4)
    # action = 100 * action
    # model = Driver(5, 5, 10, out_size)
    # model = LinearDriver(16, 500, 1, out_size)
    model = LinearConvDriver(50, 4, 4, 100, 1, out_size)
    loss_function = nn.MSELoss()
    # loss_function = nn.L1Loss()
    score = R2Score()
    # optimizer = optim.SGD(model.parameters(), lr=0.01)
    optimizer = optim.Adam(model.parameters())
    err = []
    final_loss = 0
    max_epoch = 5000
    for epoch in range(max_epoch):
        print(epoch)
        model.zero_grad()
        got = model(state)
        # print(g)
        # print(action)
        loss = loss_function(got, action)
        # err.append(loss.item())
        err.append(score(got, action).item())
        if epoch == max_epoch - 1 or epoch == 0:
            print(got)
        loss.backward()
        optimizer.step()
    print(action)
    print(err[-1])
    plt.plot(err)
    plt.show()


