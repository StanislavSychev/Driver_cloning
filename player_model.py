from model import KNNlWrapper, NNWrapper


class Player:

    def __init__(self, p=None):
        self.action = [0.0, 0.0, 0.0]
        if p is None:
            self.p = [1.0, 0.0, 2.6, 6.08333333333333,
                      5.5, 2.7, 19.0, 1.0, 8.0, 10.0,
                      11.0, 12.0, 9.0, 1.0, 38.0, 1.0,
                      2.0, 1.0, 15.0, 2.0, 1.0, 1.0]
        else:
            self.p = p
        self.model = KNNlWrapper(self.p, 1)

    def act(self):
        return "({},{},{})".format(self.action[0], self.action[1], self.action[2])

    def update(self, state):
        state = state.replace('),(', ', ')
        state = state.replace(')', '')
        state = state.replace('(', '')
        state = state.split(";")
        state = [s.split(',') for s in state]
        for j in range(len(state)):
            state[j] = [float(".".join((state[j][2*i],
                                        state[j][2*i+1])))
                        for i in range(len(state[j]) // 2)]
            state[j] = [state[j][0], state[j][2], state[j][4], state[j][6]]
        mystate = state[0]
        others = state[1:]
        others.sort(key=lambda x: (x[0] - mystate[0]) ** 2 + (x[1] - mystate[1]) ** 2)
        others = others[:3]
        for s in others:
            for item in s:
                mystate.append(item)
        self.action = self.model(mystate)
        print(self.action)


if __name__ == '__main__':
    string = "(0,0, 1,0, 0,99),(0,7, 0,0, 0,0, 0,8);(0,0, 1,0, -0,99),(0,7, 0,0, 0,0, 0,8);" \
             "(0,0, 2,0, 0,99),(0,7, 0,0, 0,0, 0,8);(10,0, 1,0, 0,99),(0,7, 0,0, 0,0, 0,8)"
    p = Player()
    p.update(string)
