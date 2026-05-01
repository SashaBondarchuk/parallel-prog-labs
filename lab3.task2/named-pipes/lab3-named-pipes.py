import struct
import logging
import sys
import os

folder = os.path.dirname(os.path.abspath(__file__))
logging.basicConfig(filename=f'{folder}/pipe_log.txt', level=logging.INFO, encoding='utf-8')

def main():
    # Connect to Windows named pipe
    with open(r'\\.\pipe\LabPipe', 'r+b') as pipe:
        # First, read iteration count
        data = pipe.read(4)
        iterations = struct.unpack('i', data)[0]
        logging.info(f"Starting {iterations} iterations")

        # Process all iterations
        for i in range(iterations):
            # Read 4 bytes (Int32)
            data = pipe.read(4)
            number = struct.unpack('i', data)[0]

            if (i + 1) % 100 == 0:
                logging.info(f"Processed {i + 1}/{iterations} iterations")

            # Send back the same bytes
            pipe.write(data)
            pipe.flush()

        logging.info(f"Completed all {iterations} iterations")

if __name__ == "__main__":
    main()