import matplotlib.pyplot as plt
import simplejson
from os import listdir

def on_map(filename):
    x = []
    y = []
    data = open(filename, 'r')
    for line in data.readlines():
        ent = line.split("\t")
        if ent[0] == 'STATE:':
            state = simplejson.loads(ent[1])
            print(state[0])
            x.append(state[1][0][2][0])
            y.append(state[1][0][2][2])
    return x, y


def map_all(file):
    x, y = on_map("parsedData/" + file)
    img = plt.imread("map.jpg")
    fig, ax = plt.subplots()
    ax.imshow(img, extent=[-400, 400, -350, 350])
    plt.scatter(x, y)
    print(x)
    fig.savefig("pics/" + file + '.png')
    # plt.show()

if __name__ == '__main__':
    for files in listdir("parsedData"):
        map_all(files)
