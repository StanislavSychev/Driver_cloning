import torch
import simplejson


def read_trajectory(filename, action_type):
    state = []
    action = []
    do_nothing = [0, 0, 0]
    didnt_move = True
    stop_move = None
    data = open("parsedData/" + filename, 'r')
    fst = None
    lst = None
    for line in data.readlines():
        ent = line.split("\t")
        if ent[0] == "ACTION:":
            if fst is None:
                fst = "ACTION"
            lst = "ACTION"
            a = simplejson.loads(ent[1])
            if a[1] == do_nothing and didnt_move:
                state = state[:-1]
                continue
            didnt_move = False
            if a[1] == do_nothing and stop_move is None:
                stop_move = len(action)
            if a[1] != do_nothing:
                stop_move = None
            action.append(torch.FloatTensor([a[1][action_type]]))
            # action.append(torch.FloatTensor([a[1][0]]))
        if ent[0] == "STATE:":
            if fst is None:
                fst = "STATE"
            lst = "STATE"
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
    # if lst == "STATE":
    #     state = state[:-1]
    # if fst == "ACTION":
    #     action = action[1:]
    # action = action[1:]
    # state = state[1:]
    if stop_move:
        action = action[:stop_move]
        state = state[:stop_move]
    state = torch.cat(state).view(len(state), -1)
    action = torch.cat(action).view(len(action), 1)
    state = state.view(state.size(0), 1, 4, 4)
    return state, action


if __name__ == '__main__':
    s, a = read_trajectory("18.1_2", 0)
