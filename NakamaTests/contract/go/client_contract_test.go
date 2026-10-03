// Client <-> Go contract. Copy into nakama/server/go/contract/ of a throwaway module copy, then:
//   UBER_CS_VECTORS=cs.json UBER_GO_VECTORS=go.json go test ./contract/
package contract

import (
	"bytes"
	"encoding/base64"
	"encoding/hex"
	"encoding/json"
	"os"
	"testing"

	"github.com/TeamUberStrike/photon-migration/nakama/server/go/internal/rmi"
	"github.com/TeamUberStrike/photon-migration/nakama/server/go/internal/roomcore"
	"github.com/TeamUberStrike/photon-migration/nakama/server/go/rpc"
	"github.com/TeamUberStrike/photon-migration/nakama/server/go/wire"
)

type csVectors struct {
	Envelopes []struct {
		Op      int64  `json:"op"`
		NetID   int16  `json:"netId"`
		Method  byte   `json:"method"`
		Target  int32  `json:"target"`
		ArgsHex string `json:"argsHex"`
		Hex     string `json:"hex"`
	} `json:"envelopes"`
	Op66 []struct {
		JSON string `json:"json"`
		M    int    `json:"m"`
		I    int16  `json:"i"`
		AHex string `json:"aHex"`
		P    string `json:"p"`
	} `json:"op66"`
	RoomJoin []struct {
		JSON    string `json:"json"`
		MetaHex string `json:"metaHex"`
	} `json:"roomJoin"`
	Time []struct {
		JSON string `json:"json"`
		C    int64  `json:"c"`
	} `json:"time"`
	JoinAck []struct {
		Actor    int32  `json:"actor"`
		Count    int32  `json:"count"`
		ServerMs int32  `json:"serverMs"`
		Init     bool   `json:"init"`
		Number   int32  `json:"number"`
		Server   string `json:"server"`
		Hex      string `json:"hex"`
	} `json:"joinAck"`
}

func unhex(t *testing.T, s string) []byte {
	t.Helper()
	b, err := hex.DecodeString(s)
	if err != nil {
		t.Fatal(err)
	}
	return b
}

func TestClientVectors(t *testing.T) {
	path := os.Getenv("UBER_CS_VECTORS")
	if path == "" {
		t.Skip("UBER_CS_VECTORS not set")
	}
	raw, err := os.ReadFile(path)
	if err != nil {
		t.Fatal(err)
	}
	var v csVectors
	if err := json.Unmarshal(raw, &v); err != nil {
		t.Fatal(err)
	}
	if len(v.Envelopes) == 0 || len(v.Op66) == 0 || len(v.RoomJoin) == 0 || len(v.Time) == 0 || len(v.JoinAck) == 0 {
		t.Fatal("empty client vectors")
	}

	for _, e := range v.Envelopes {
		got, err := rmi.ParseEnvelope(e.Op, unhex(t, e.Hex))
		if err != nil {
			t.Fatalf("op %d: %v", e.Op, err)
		}
		if got.NetworkID != e.NetID || got.MethodID != e.Method || !bytes.Equal(got.Args, unhex(t, e.ArgsHex)) {
			t.Fatalf("op %d: %+v vs %+v", e.Op, got, e)
		}
		if e.Op == rmi.OpToPlayer && got.Target != e.Target {
			t.Fatalf("op 80 target %d != %d", got.Target, e.Target)
		}
		if !bytes.Equal(got.Bytes(e.Op), unhex(t, e.Hex)) {
			t.Fatalf("op %d: Go re-encode differs", e.Op)
		}
	}

	for _, c := range v.Op66 {
		var in rpc.Op66Request
		if err := json.Unmarshal([]byte(c.JSON), &in); err != nil {
			t.Fatalf("op66 %s: %v", c.JSON, err)
		}
		a, err := base64.StdEncoding.DecodeString(in.A)
		if err != nil || in.M != c.M || in.I != c.I || in.P != c.P || !bytes.Equal(a, unhex(t, c.AHex)) {
			t.Fatalf("op66 %s -> %+v (%v)", c.JSON, in, err)
		}
	}

	for _, c := range v.RoomJoin {
		var in rpc.RoomJoinRequest
		if err := json.Unmarshal([]byte(c.JSON), &in); err != nil {
			t.Fatal(err)
		}
		m, err := base64.StdEncoding.DecodeString(in.Meta)
		if err != nil || !bytes.Equal(m, unhex(t, c.MetaHex)) {
			t.Fatalf("room join %s", c.JSON)
		}
	}

	for _, c := range v.Time {
		var in rpc.TimeRequest
		if err := json.Unmarshal([]byte(c.JSON), &in); err != nil || in.C != c.C {
			t.Fatalf("time %s -> %+v %v", c.JSON, in, err)
		}
	}

	for _, c := range v.JoinAck {
		j := roomcore.JoinAck{ActorID: c.Actor, Count: c.Count, ServerMs: c.ServerMs, InitRoom: c.Init, RoomID: wire.NewRoomID(c.Number, c.Server)}
		if got := hex.EncodeToString(j.Bytes()); got != c.Hex {
			t.Fatalf("join ack Go %s != client layout %s", got, c.Hex)
		}
	}
}

func TestEmitGoVectors(t *testing.T) {
	path := os.Getenv("UBER_GO_VECTORS")
	if path == "" {
		t.Skip("UBER_GO_VECTORS not set")
	}
	type item = map[string]any
	marshal := func(v any) string {
		b, err := json.Marshal(v)
		if err != nil {
			t.Fatal(err)
		}
		return string(b)
	}
	d := []byte{1, 2, 255}
	out := map[string][]item{}

	for _, r := range []rpc.Op66Reply{
		{RC: 0, I: 7, D: base64.StdEncoding.EncodeToString(d)},
		{RC: 0, I: -5},
		{RC: 1, I: 3, Msg: "not allowed"},
		{RC: 1, I: 2, Msg: "The current version '1.6' of your client is outdated", DC: true},
	} {
		dh := ""
		if r.D != "" {
			raw, _ := base64.StdEncoding.DecodeString(r.D)
			dh = hex.EncodeToString(raw)
		}
		out["op66Reply"] = append(out["op66Reply"], item{"json": marshal(r), "rc": r.RC, "i": r.I, "dHex": dh, "msg": r.Msg, "dc": r.DC})
	}

	for _, r := range []rpc.RoomJoinReply{
		{RC: 0, Match: "5f2c1b9e-3c49-4b3c-9d67-0f5e2a7d8c11.uber-nakama-1", Number: 101},
		{RC: 1, Number: 105, Msg: "Game doesn't exist anymore!"},
	} {
		out["roomJoinReply"] = append(out["roomJoinReply"], item{"json": marshal(r), "rc": r.RC, "match": r.Match, "number": r.Number})
	}

	tr := rpc.TimeReply{C: 123456789012, S: 2147483647}
	out["timeReply"] = append(out["timeReply"], item{"json": marshal(tr), "c": tr.C, "s": tr.S})

	for _, j := range []roomcore.JoinAck{
		{ActorID: 7, Count: 3, ServerMs: 99, InitRoom: true, RoomID: wire.NewRoomID(101, "127.0.0.1:20101")},
		{ActorID: 1, Count: 1, ServerMs: 2147483647, RoomID: wire.NewRoomID(88, "127.0.0.1:20088")},
	} {
		w := &wire.Writer{}
		w.RoomID(j.RoomID)
		out["joinAck"] = append(out["joinAck"], item{"hex": hex.EncodeToString(j.Bytes()), "actor": j.ActorID, "count": j.Count, "serverMs": j.ServerMs, "init": j.InitRoom, "roomHex": hex.EncodeToString(w.B)})
	}

	for _, e := range []struct {
		net    int16
		method byte
		args   []any
	}{
		{rmi.ClassClientSync, roomcore.ClientRecieveID, []any{int32(3), int16(4)}},
		{rmi.ClassComm, 25, []any{int32(5), "hi"}},
		{rmi.ClassDM, 83, nil},
		{-1, 1, []any{true}},
	} {
		b, err := rmi.Event(e.net, e.method, e.args...)
		if err != nil {
			t.Fatal(err)
		}
		out["events"] = append(out["events"], item{"hex": hex.EncodeToString(b), "netId": e.net, "method": e.method, "argsHex": hex.EncodeToString(b[3:])})
	}

	for rc := 1; rc <= 5; rc++ {
		out["reasons"] = append(out["reasons"], item{"reason": roomcore.Reason(rc), "rc": rc})
	}

	b, err := json.MarshalIndent(out, "", " ")
	if err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(path, b, 0o644); err != nil {
		t.Fatal(err)
	}
}
