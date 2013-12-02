package mavlink

type newMessageFunc func() Message

type Message interface {
        ID() uint8
        Size() uint8
}

var messageFactory = map[uint8]newMessageFunc{
/*MESSAGEFACTORY*/
}

/*ENUMS*/

/*MESSAGES*/