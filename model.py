import torch
import torch.nn as nn
from attention import Attention

lstm = nn.LSTM(3, 2)  # Input dim is 3, output dim is 3
inputs = [torch.randn(1, 3) for _ in range(5)]  # make a sequence of length 5
print(inputs)
hidden0 = (torch.randn(1, 1, 2), torch.randn(1, 1, 2))

hidden = hidden0
for i in inputs:
    # Step through the sequence one element at a time.
    # after each step, hidden contains the hidden state.
    out, hidden = lstm(i.view(1, 1, -1), hidden)
    # print(out)
    # print(hidden)
# print(out)

hidden = hidden0
inputs = torch.cat(inputs).view(len(inputs), 1, -1)
out, hidden = lstm(inputs, hidden)
print(inputs)
# print(out)
# print(out.view(len(inputs), -1))
# print(hidden)