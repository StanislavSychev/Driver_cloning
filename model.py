import torch.nn as nn


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