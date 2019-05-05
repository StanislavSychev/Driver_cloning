import torch.nn as nn
import torch.optim as optim
import matplotlib.pyplot as plt
from r_score import R2Score
from data_reader import read_trajectory
from model import LinearConvDriver


if __name__ == '__main__':
    state, action = read_trajectory("18.1_2", 0)
    # model = Driver(5, 5, 10, out_size)
    # model = LinearDriver(16, 500, 1, out_size)
    model = LinearConvDriver(50, 4, 4, 100, 1, 1)
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
        loss = loss_function(got, action)
        err.append(score(got, action).item())
        if epoch == max_epoch - 1 or epoch == 0:
            print(got)
        loss.backward()
        optimizer.step()
    print(action)
    print(err[-1])
    plt.plot(err)
    plt.show()
