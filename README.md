# PipeDelimited-https
Creates an HL7 pipe-delimited message that can be submitted via https POST.

Thanks to @StefanHeesch for the Message and Segment classes that conform to HL7 V2.x standards.
This program creates a string that is serialized as a HL7 pipe-delmited message, the string is put into a byte and POSTs to an HTTPS endpoint.
POSTing to an http endpoint is straightfoward, but using https can be tricky with the WebResponse class. 
I hope this application be of use to you.

Bryndon
