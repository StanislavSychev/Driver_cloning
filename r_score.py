import torch
import torch.nn as nn


class R2Score:
    def __init__(self):
        self.mse = nn.MSELoss()

    def __call__(self, got, exp):
        u = self.mse(got, exp)
        v = torch.mean((exp - torch.mean(exp)) ** 2)
        return 1 - u / v


if __name__ == '__main__':
    x = torch.rand(3, 3)
    s = R2Score()
    print(s(x, x))
