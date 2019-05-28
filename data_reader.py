import torch
import simplejson
import pandas as pd
import numpy as np
from os import listdir

wait = 0
length = 0


def read_trajectory(fold, filename, action_type):
    global length, wait
    length += 1
    state = []
    action = []
    do_nothing = [0, 0, 0]
    didnt_move = True
    stop_move = None
    data = open(fold + "/" + filename, 'r')
    fst = None
    lst = None
    start = 2
    for line in data.readlines():
        ent = line.split("\t")
        if ent[0] == "ACTION:":
            if fst is None:
                fst = "ACTION"
            lst = "ACTION"
            a = simplejson.loads(ent[1])
            if a[1] == do_nothing and didnt_move or (start > 0):
                wait += 1
                start -= 1
                state = state[:-1]
                continue
            didnt_move = False
            if a[1] == do_nothing and stop_move is None:
                stop_move = len(action)
            if a[1] != do_nothing:
                stop_move = None
            if action_type == 3:
                action.append(torch.FloatTensor([a[1][1] - a[1][2]]))
            else:
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
    if lst == "STATE":
        state = state[:-1]
    # if fst == "ACTION":
    #     action = action[1:]
    # action = action[1:]
    # state = state[1:]
    if stop_move:
        action = action[:stop_move]
        state = state[:stop_move]
    l = [s.size(0) for s in state]
    for i in range(len(l)):
        if l[i] < 16:
            del state[i]
            del action[i]
    state = torch.cat(state).view(len(state), -1)
    action = torch.cat(action).view(len(action), 1)
    return state, action


def read_driver_trajectories(fold, dname, action_type):
    s = []
    a = []
    for files in listdir(fold):
        if dname in files and files[-1] != '1':
            st, ac = read_trajectory(fold, files, action_type)
            s.append(st)
            a.append(ac)
    l = 0
    for ac in a:
        l += ac.size(0)
    a = torch.cat(a).view(l, -1)
    s = torch.cat(s).view(l, -1)
    return s, a


def read_all(fold, action_type):
    qest = pd.read_csv("preQuestionnaire.csv", index_col=0)
    qest = qest.fillna(0)
    qest = qest.replace({'gender': r'^[f|F].*'}, {'gender': 1}, regex=True)
    qest = qest.replace({'gender': r'^[m|M].*'}, {'gender': 2}, regex=True)
    qest = qest.replace({'gender': r'^h.*'}, {'gender': 0}, regex=True)

    def get_by_id(driver_id):
        row = qest[qest.ID == driver_id]
        row = row.drop(columns=['ID'])
        return row.values

    s = []
    a = []
    for files in listdir(fold):
        if files[-1] == '1':
            continue
        st, ac = read_trajectory(fold, files, action_type)
        p = get_by_id(float(files[:-2]))
        p = np.tile(p, (st.size(0), 1))
        p = torch.FloatTensor(p)
        st = torch.cat((p, st), 1)
        a.append(ac)
        s.append(st)
    l = 0
    for ac in a:
        l += ac.size(0)
    a = torch.cat(a).view(l, -1)
    s = torch.cat(s).view(l, -1)
    return s, a


def read_all_trajectories(fold, action_type):
    s = []
    a = []
    for files in listdir(fold):
        if files[-1] == '1':
            continue
        st, ac = read_trajectory(fold, files, action_type)
        a.append(ac)
        s.append(st)
    l = 0
    for ac in a:
        l += ac.size(0)
    a = torch.cat(a).view(l, -1)
    s = torch.cat(s).view(l, -1)
    return s, a


if __name__ == '__main__':
    s, a = read_all("parsedData", 0)
    # p, s = s.split(22, 2)
    print(wait / length)
