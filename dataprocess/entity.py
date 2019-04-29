class RawEntity:
    lines = []

    def __init__(self, string):
        self.lines = string.split('\n')[:-1]
        self.type = self.get_type_fst()

    def get_type_fst(self):
        fst_line = self.lines[1].split('\t')[0]
        if fst_line == "Index":
            return "ID"
        if fst_line == "CAR" and self.lines[2].split('\t')[0] == "POS":
            return "STATE"
        if fst_line == "Steer":
            return "ACTION"
        return "UNDEFINED"

    def get_type(self):
        return self.type

    def get_time(self):
        for line in self.lines:
            if line.split('\t')[0] == "time":
                return line.split('\t')[1]
        return "unknown"

    def to_dict(self) -> dict:
        if self.type == "UNDEFINED" or self.type == "SCEN":
            return {}
        res = {}
        for line in self.lines:
            res[line.split("\t")[0]] = line.split("\t")[1]
        return res


class Entity:
    def __init__(self, string):
        raw = RawEntity(string)
        self.values = raw.to_dict()
        self.type = raw.get_type()

    def get_value(self):
        if self.type == "ID":
            return self.values["Response"]
        if self.type == "ACTION":
            return [self.get_time(), [float(self.values["Steer"]),
                                      float(self.values["Throttle"]),
                                      float(self.values["Brake"])]]
        if self.type == "STATE":
            pos = self.values["POS"][1:-1].split(", ")
            rot = self.values["ROT"][1:-1].split(", ")
            name = self.values["CAR"].split(' ')[0]
            signal = self.values["Signal"]
            if signal == "None":
                signal = 0
            if signal == "Left":
                signal = -1
            if signal == "Right":
                signal = 1
            pos = [float(a) for a in pos]
            rot = [float(a) for a in rot]
            return [self.get_time(), [name, signal, pos, rot]]
        return []

    def get_type(self):
        return self.type

    def get_sceen(self):
        if "SCENE" in self.values.keys():
            return self.values["SCENE"]
        return None

    def get_time(self):
        if "time" in self.values.keys():
            return float(self.values["time"].replace(",", ""))
        return -1
