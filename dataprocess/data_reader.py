from os import listdir, remove, getcwd
from os.path import join, split
from dataprocess.entity import Entity
import simplejson


class DataReader:

    @staticmethod
    def print_action(actions, write):
        if actions:
            s = 0
            t = 0
            b = 0
            time = 0
            n = 0
            for action in actions:
                time += action[0]
                s += action[1][0]
                t += action[1][1]
                b += action[1][2]
                n += 1
            s = s / n
            t = t / n
            b = b / n
            time = time / n
            write.write("ACTION:\t" + simplejson.dumps([time, [s, t, b]]) + "\n")

    @staticmethod
    def print_state(drivers, time, write):
        if drivers:
            write.write("STATE:\t" + simplejson.dumps([time, drivers]) + "\n")

    @staticmethod
    def parse(read_dir, write_dir):
        print(getcwd())
        for files in listdir(write_dir):
            remove(join(write_dir, files))
        for files in listdir(read_dir):
            data = open(join(read_dir, files), 'r')
            current_entity = -1
            string = ""
            entitys = []
            for line in data.readlines():
                list_line = line.split('\t')
                if int(list_line[0]) == current_entity:
                    string = string + '\t'.join(list_line[1:])
                else:
                    if current_entity != -1:
                        entitys.append(Entity(string))
                    current_entity += 1
                    string = '\t'.join(list_line[1:])
            # write = open(join(write_dir, files), 'w')
            entitys = [ent for ent in entitys if ent.get_type() != "UNDEFINED"]
            cur_type = "UNDEFINED"
            actions = []
            drivers = []
            time = 0
            trajectory = 0
            driver_id = None
            scene = None
            for ent in entitys:
                if ent.get_sceen() and ent.get_sceen() != scene:
                    scene = ent.get_sceen()
                    trajectory += 1
                    write = open(join(write_dir, str(driver_id) + "_" + str(trajectory)), 'w')
                if ent.get_type() == "ID":
                    driver_id = ent.get_value()
                    # write = open(join(write_dir, str(ent.get_value()) + "_" + str(trajectory)), 'w')
                    # print(ent.get_value())
                if cur_type == ent.get_type() and cur_type == "ACTION":
                    actions.append(ent.get_value())
                    continue
                if cur_type == ent.get_type() and cur_type == "STATE":
                    drivers.append(ent.get_value()[1])
                    continue
                if ent.get_type() == "ACTION":
                    actions = [ent.get_value()]
                    cur_type = "ACTION"
                    DataReader.print_state(drivers, time, write)
                    drivers = []
                if ent.get_type() == "STATE":
                    drivers = [ent.get_value()[1]]
                    time = ent.get_time()
                    cur_type = "STATE"
                    DataReader.print_action(actions, write)
                    actions = []
                    # write.write(ent.get_type() + ":\t" + simplejson.dumps(ent.get_value()) + "\n")
            DataReader.print_action(actions, write)
            DataReader.print_state(drivers, time, write)
            write.close()


if __name__ == '__main__':
    cur_dir = getcwd()
    cur_dir = split(cur_dir)[0]
    DataReader.parse(join(cur_dir, "rawData"), join(cur_dir, "parsedData"))
